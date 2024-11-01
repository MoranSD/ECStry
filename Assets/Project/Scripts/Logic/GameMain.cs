using ECS;
using UnityEngine;

namespace Logic
{
    public class GameMain : MonoBehaviour
    {
        [SerializeField] private GameObject playerView;

        private ECSRoot ecsRoot;

        private void Awake()
        {
            ecsRoot = new();
            ecsRoot.Initialize();
            ecsRoot.DefineNewEntity(new PlayerEntity(playerView));
        }

        private void Update()
        {
            ecsRoot.ExecuteAll<PlayerInputSystem>();
            ecsRoot.ExecuteAll<MoveSystem>();
        }
    }

    public class PlayerEntity : ECSEntity
    {
        public PlayerEntity(GameObject view)
        {
            Define<TagPlayer>();
            Define<TranslationComponent>().Transform = view.transform;
            Define<MoveComponent>().Speed = 5;
        }
    }

    public class TagPlayer : EntityComponent
    {

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

    public class PlayerInputSystem : BaseSystem
    {
        public override void Execute(ECSEntity entity)
        {
            if(entity.Is<TagPlayer>())
            {
                if(entity.Is<MoveComponent>(out var moveComponent))
                {
                    moveComponent.direction = new Vector3 (Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                }
            }
        }
    }

    public class MoveSystem : BaseSystem
    {
        public override void Execute(ECSEntity entity)
        {
            if (entity.Is<MoveComponent>(out var moveComponent) && entity.Is<TranslationComponent>(out var translationComponent))
            {
                translationComponent.Transform.position += moveComponent.direction * moveComponent.Speed * Time.deltaTime;
            }
        }
    }
}