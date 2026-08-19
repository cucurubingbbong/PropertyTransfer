using UnityEngine;

/// <summary>
/// 특성 상태에 맞춰 Rigidbody와 Physic Material 값을 적용한다.
/// </summary>

[RequireComponent(typeof(Rigidbody))]
public class PhysicsPropertyApplier : MonoBehaviour
{
    [SerializeField] private PhysicsMaterial originPhysicsMaterial = null;
    [SerializeField] private PhysicsMaterial physicsMaterial = null;

    [SerializeField] Rigidbody rigidBody = null;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        GeneratePhysicsMaterial();
    }

    void GeneratePhysicsMaterial()
    {
        if (originPhysicsMaterial == null)
        {
            Debug.LogWarning("원본 물리재질이 없음");
            return;
        }
        physicsMaterial = new PhysicsMaterial();
        physicsMaterial.dynamicFriction = originPhysicsMaterial.dynamicFriction;
        physicsMaterial.staticFriction = originPhysicsMaterial.staticFriction;
        physicsMaterial.bounciness = originPhysicsMaterial.bounciness;
        physicsMaterial.frictionCombine = originPhysicsMaterial.frictionCombine;
        physicsMaterial.bounceCombine = originPhysicsMaterial.bounceCombine;
    }


    void SetPhysicsMaterial(PhysicsMaterial material)
    {
        if (material == null)
        {
            Debug.LogWarning("물리재질이 없음");
            return;
        }

        physicsMaterial = material;
    }

    public void ApplyProperty(PropertyData property)
    {
        if (property == null)
        {
            Debug.LogWarning("프로퍼티 데이터가 빔");
            return;
        }

        switch (property.PropertyType)
        {
            case PropertyType.Weight:
                rigidBody.mass = property.Value;
                break;
            case PropertyType.Friction:
                physicsMaterial.staticFriction = property.Value;
                break;
            case PropertyType.Elasticity:
                physicsMaterial.bounciness = property.Value;
                break;
            default:
                Debug.LogWarning($"지원하는 않는 물리타입: {property.PropertyType}");
                break;
        }
    }
}
