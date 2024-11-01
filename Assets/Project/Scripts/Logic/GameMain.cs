using ECS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace Logic
{
    public class GameMain : MonoBehaviour
    {
        [SerializeField] private GameObject playerView;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject[] enemies;

        public static ECSRoot EcsRoot { get; private set; }

        private void Awake()
        {
            EcsRoot = new();
            EcsRoot.Initialize();
            EcsRoot.AddEntity(new PlayerEntity(playerView, bulletPrefab));

            foreach (var enemy in enemies)
                EcsRoot.AddEntity(new EnemyEntity(enemy));
        }

        private void Update()
        {
            EcsRoot.Execute<PlayerInputSystem>();
            EcsRoot.Execute<MoveSystem>();
            EcsRoot.Execute<BulletSpawnSystem>();
            EcsRoot.Execute<CollideSystem>();
            EcsRoot.Execute<BulletKillSystem>();
            EcsRoot.Execute<LifeTimeSystem>();
        }

        private void OnDestroy()
        {
            EcsRoot = null;
        }
    }

    public class EnemyEntity : ECSEntity
    {
        public EnemyEntity(GameObject view)
        {
            Define<CollideableComponent>().Collider = view.GetComponent<Collider>();
            Define<KillableComponent>().View = view;
        }
    }

    public class PlayerEntity : ECSEntity
    {
        public PlayerEntity(GameObject view, GameObject bulletPrefab)
        {
            Define<TagPlayer>();
            Define<TranslationComponent>().Transform = view.transform;
            Define<MoveComponent>().Speed = 5;
            Define<BulletSpawnComponent>().SpawnPivot = view.transform;
            Define<BulletSpawnComponent>().BulletPrefab = bulletPrefab;
            Define<BulletSpawnComponent>().BulletOwner = BulletOwner.Hero;
            Define<TagBulletOwner>().Owner = BulletOwner.Hero;
        }
    }

    public class BulletEntity : ECSEntity
    {
        public BulletEntity(GameObject view, BulletOwner owner)
        {
            Define<TagBullet>();
            Define<TranslationComponent>().Transform = view.transform;
            Define<MoveComponent>().direction = view.transform.forward;
            Define<MoveComponent>().Speed = 15;
            Define<LifeTimeComponent>().RemainingLifeTime = 3; 
            Define<LifeTimeComponent>().View = view;
            Define<KillableComponent>().View = view;
            Define<TagBulletOwner>().Owner = owner;
            Define<CollideComponent>().Radius = 1;
            Define<CollideComponent>().Pivot = view.transform;
        }
    }

    public class TagPlayer : EntityComponent
    {

    }

    public class TagBullet : EntityComponent
    {
    }

    public enum BulletOwner
    {
        Hero,
        Enemy,
    }

    public class TagBulletOwner : EntityComponent
    {
        public BulletOwner Owner;
    }

    public class LifeTimeComponent : EntityComponent
    {
        public float RemainingLifeTime;
        public GameObject View;
    }
    public class BulletSpawnComponent : EntityComponent
    {
        public Transform SpawnPivot;
        public GameObject BulletPrefab;
        public BulletOwner BulletOwner;
        public int ToSpawn;
    }

    public class BulletSpawnRateComponent : EntityComponent
    {
        public float SpawnRate;
        public float CurrentSpawnDelay;
    }

    public class MoveComponent : EntityComponent
    {
        public Vector3 direction;
        public float Speed;
    }

    public class TranslationComponent : EntityComponent
    {
        public Transform Transform;
    }

    public class CollideComponent : EntityComponent
    {
        public Transform Pivot;
        public float Radius;
        public List<Collider> Collisions;
    }

    public class CollideableComponent : EntityComponent
    {
        public Collider Collider;
    }

    public class KillableComponent : EntityComponent
    {
        public GameObject View;
    }

    public class CollideSystem : BaseSystem
    {
        public override void Execute(IEnumerable<ECSEntity> entities)
        {
            foreach (var entity in entities)
            {
                if (entity.Is<CollideComponent>(out var collideComponent))
                {
                    collideComponent.Collisions = Physics.OverlapSphere(collideComponent.Pivot.position, collideComponent.Radius).ToList();
                }
            }
        }
    }

    public class BulletKillSystem : BaseSystem
    {
        public override void Execute(IEnumerable<ECSEntity> entities)
        {
            var entitiesToKill = new List<ECSEntity>();

            foreach (var entity in entities)
            {
                if (entity.Is<TagBullet>() == false) continue;
                if (entity.Is<CollideComponent>(out var collideComponent) == false) continue;
                if (collideComponent.Collisions == null || collideComponent.Collisions.Count == 0) continue;

                var allEntities = GameMain.EcsRoot.GetAllEntities();

                foreach (var checkEntity in allEntities)
                {
                    if (checkEntity == entity) continue;

                    if (checkEntity.Is<CollideableComponent>(out var collideableComponent))
                    {
                        if (collideComponent.Collisions.Contains(collideableComponent.Collider))
                        {
                            if (checkEntity.Is<KillableComponent>())
                                entitiesToKill.Add(checkEntity);

                            entitiesToKill.Add(entity);
                        }
                    }
                }
            }

            foreach (var entity in entitiesToKill)
            {
                GameMain.EcsRoot.RemoveEntity(entity);
                GameObject.Destroy(entity.Get<KillableComponent>().View);
            }
        }
    }

    public class LifeTimeSystem : BaseSystem
    {
        public override void Execute(IEnumerable<ECSEntity> entities)
        {
            var entitiesToRemove = new List<ECSEntity>(entities.Count());

            foreach (var entity in entities)
            {
                if (entity.Is<LifeTimeComponent>(out var component))
                {
                    if (component.RemainingLifeTime > 0)
                    {
                        component.RemainingLifeTime -= Time.deltaTime;
                    }

                    if (component.RemainingLifeTime <= 0)
                    {
                        entitiesToRemove.Add(entity);
                    }
                }
            }

            foreach (var entity in entitiesToRemove)
            {
                GameMain.EcsRoot.RemoveEntity(entity);
                GameObject.Destroy(entity.Get<LifeTimeComponent>().View);
            }
        }
    }

    public class PlayerInputSystem : BaseSystem
    {
        public override void Execute(IEnumerable<ECSEntity> entities)
        {
            foreach(var entity in entities)
            {
                if (entity.Is<TagPlayer>())
                {
                    if (entity.Is<MoveComponent>(out var moveComponent))
                    {
                        moveComponent.direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                    }

                    if (Input.GetKeyDown(KeyCode.Space) && entity.Is<BulletSpawnComponent>(out var bulletSpawnComponent))
                    {
                        bulletSpawnComponent.ToSpawn++;
                    }
                }
            }
        }
    }

    public class BulletSpawnSystem : BaseSystem 
    {
        public override void Execute(IEnumerable<ECSEntity> entities)
        {
            var bulletsToCreate = new List<BulletSpawnComponent>();

            foreach (var entity in entities)
            {
                if (entity.Is<BulletSpawnComponent>(out var component) == false) continue;

                bool hasSpawnRateComponent = entity.Is<BulletSpawnRateComponent>(out var bulletSpawnRateComponent);

                if (hasSpawnRateComponent)
                {
                    if (bulletSpawnRateComponent.CurrentSpawnDelay > 0)
                    {
                        bulletSpawnRateComponent.CurrentSpawnDelay -= Time.deltaTime;
                        continue;
                    }
                }

                if(component.ToSpawn > 0)
                {
                    bulletsToCreate.Add(component);
                    component.ToSpawn--;

                    if (hasSpawnRateComponent)
                    {
                        bulletSpawnRateComponent.CurrentSpawnDelay += bulletSpawnRateComponent.SpawnRate;

                        if (bulletSpawnRateComponent.CurrentSpawnDelay > 0)
                            break;
                    }
                }
            }

            foreach (var addEntity in bulletsToCreate)
            {
                var bulletView = GameObject.Instantiate(addEntity.BulletPrefab);
                bulletView.transform.position = addEntity.SpawnPivot.position;
                bulletView.transform.rotation = addEntity.SpawnPivot.rotation;

                var bulletEntity = new BulletEntity(bulletView, addEntity.BulletOwner);
                GameMain.EcsRoot.AddEntity(bulletEntity);
            }
        }
    }

    public class MoveSystem : BaseSystem
    {
        public override void Execute(IEnumerable<ECSEntity> entities)
        {
            foreach (var entity in entities)
            {
                if (entity.Is<MoveComponent>(out var moveComponent) && entity.Is<TranslationComponent>(out var translationComponent))
                {
                    translationComponent.Transform.position += moveComponent.direction * moveComponent.Speed * Time.deltaTime;
                }
            }
        }
    }
}