using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;

public class EnemyConnector : MonoBehaviour
{
    // ID của cái xác (Entity) mà não bộ này điều khiển
    public Entity MyEntity { get; private set; }
    
    private EntityManager _entityManager;
    private bool _hasFoundBody = false;

    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        _entityManager = world.EntityManager;
    }

    void Update()
    {
        // 1. Nếu chưa tìm thấy xác thì đi tìm
        if (!_hasFoundBody)
        {
            FindMyBody();
            return;
        }

        // 2. Nếu tìm thấy rồi thì ĐỒNG BỘ VỊ TRÍ (Để cục não đi theo cái xác)
        // Điều này giúp Debug dễ hơn, và nếu bạn gắn Camera vào não, Camera sẽ đi theo Enemy.
        if (_entityManager.Exists(MyEntity))
        {
            var entityTransform = _entityManager.GetComponentData<LocalTransform>(MyEntity);
            transform.position = entityTransform.Position;
        }
    }

    void FindMyBody()
    {
        // Tìm tất cả Entity có EnemyTag
        var query = _entityManager.CreateEntityQuery(typeof(EnemyTag));
        
        if (!query.IsEmpty)
        {
            // TẠM THỜI: Lấy đại con đầu tiên tìm được.
            // (Lưu ý: Nếu có 10 con, cách này sẽ lỗi, nhưng test 1 con thì OK)
            MyEntity = query.GetSingletonEntity();
            _hasFoundBody = true;
            Debug.Log("Đã kết nối não với thân xác Entity!");
        }
    }
}