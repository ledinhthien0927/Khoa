using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Physics; // Thư viện Vật lý quan trọng

[BurstCompile]
public partial struct EnemyMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. Query lấy dữ liệu
        // - RefRW<LocalTransform>: Cần GHI (Write) để xoay mặt Enemy (Rotation).
        // - RefRW<PhysicsVelocity>: Cần GHI (Write) để gán vận tốc di chuyển.
        // - RefRO<MoveTarget> & RefRO<MoveSpeed>: Chỉ cần ĐỌC (Read Only).
        foreach (var (transform, velocity, target, speed) in 
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRO<MoveTarget>, RefRO<MoveSpeed>>()
                 .WithAll<EnemyTag>())
        {
            // Lấy vận tốc hiện tại (chủ yếu để giữ lại trục Y - trọng lực)
            float3 currentVelocity = velocity.ValueRO.Linear;
            
            // Mặc định vận tốc mong muốn (X, Z) là 0 (đứng yên)
            float3 desiredLinearVelocity = float3.zero;
            
            // Giữ nguyên vận tốc Y để Enemy có thể rơi tự do hoặc nhảy
            desiredLinearVelocity.y = currentVelocity.y;

            // Kiểm tra xem có mục tiêu không
            if (target.ValueRO.HasTarget)
            {
                // Tính toán hướng: Đích đến - Vị trí hiện tại
                float3 direction = target.ValueRO.Position - transform.ValueRO.Position;

                // Loại bỏ trục Y trong tính toán hướng (chỉ xét mặt phẳng ngang)
                direction.y = 0;

                // Tính khoảng cách bình phương (nhanh hơn tính căn bậc 2)
                float distanceSq = math.lengthsq(direction);

                // Ngưỡng dừng lại (Stop Distance): 0.5f * 0.5f = 0.25f
                if (distanceSq > 0.25f)
                {
                    // Chuẩn hóa vector hướng (độ dài = 1)
                    float3 moveDir = math.normalize(direction);

                    // --- XỬ LÝ DI CHUYỂN (PHYSICS) ---
                    // Gán vận tốc mới cho trục X và Z
                    desiredLinearVelocity.x = moveDir.x * speed.ValueRO.Value;
                    desiredLinearVelocity.z = moveDir.z * speed.ValueRO.Value;

                    // --- XỬ LÝ XOAY MẶT (ROTATION) ---
                    // Chỉ xoay nếu vector hướng hợp lệ (tránh lỗi chia cho 0)
                    if (!moveDir.Equals(float3.zero))
                    {
                        // Tạo quaternion hướng về phía moveDir, trục lên là Y (Up)
                        quaternion targetRotation = quaternion.LookRotation(moveDir, math.up());
                        
                        // Gán trực tiếp vào Transform để xoay nhân vật
                        // (Lưu ý: Rotation Slerp để mượt hơn có thể thêm sau, ở đây dùng gán cứng cho đơn giản)
                        transform.ValueRW.Rotation = targetRotation;
                    }
                }
                else
                {
                    // Đã đến nơi -> Vận tốc X, Z tự động bằng 0 (theo khai báo desiredLinearVelocity ban đầu)
                    // Logic dừng hẳn: Có thể thêm logic thông báo "Đã đến nơi" tại đây nếu cần
                }
            }

            // CẬP NHẬT VÀO HỆ THỐNG VẬT LÝ
            // Ghi đè vận tốc tuyến tính mới vào Component
            velocity.ValueRW.Linear = desiredLinearVelocity;
            
            // Reset vận tốc góc (Angular Velocity) để tránh Enemy bị xoay vòng tròn do va chạm
            velocity.ValueRW.Angular = float3.zero;
        }
    }
}