using UnityEngine;

/// <summary>
/// 특성 상태에 맞춰 Rigidbody와 Physic Material 값을 적용한다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PhysicsPropertyApplier : MonoBehaviour
{
    [SerializeField] private PhysicsMaterial originPhysicsMaterial = null;
    [SerializeField] private PhysicsMaterial physicsMaterial = null;

    [SerializeField] private Rigidbody rigidBody = null;
    [SerializeField] private Collider targetCollider = null;

    /// <summary>
    /// 특성이 적용되기 전 Rigidbody의 원본 무게
    /// </summary>
    private float originMass;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        targetCollider = GetComponent<Collider>();

        originMass = rigidBody.mass;

        GeneratePhysicsMaterial();
    }

    /// <summary>
    /// 원본 물리재질을 기준으로 해당 오브젝트만 사용할 물리재질을 생성한다.
    /// </summary>
    private void GeneratePhysicsMaterial()
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

        // 생성한 물리재질을 실제 Collider에 적용한다.
        targetCollider.sharedMaterial = physicsMaterial;
    }

    /// <summary>
    /// 현재 가지고 있는 모든 특성을 기준으로 물리 상태를 다시 적용한다.
    /// 기존 특성 효과를 원본 상태로 되돌린 뒤 현재 특성을 적용한다.
    /// </summary>
    public void ApplyProperties(PropertyData[] properties)
    {
        ResetProperties();

        if (properties == null)
            return;

        for (int i = 0; i < properties.Length; i++)
        {
            if (properties[i] == null)
                continue;

            ApplyProperty(properties[i]);
        }
    }

    /// <summary>
    /// 하나의 특성을 물리 상태에 적용한다.
    /// </summary>
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

    /// <summary>
    /// 모든 물리 특성을 원본 상태로 되돌린다.
    /// </summary>
    private void ResetProperties()
    {
        rigidBody.mass = originMass;

        if (originPhysicsMaterial == null || physicsMaterial == null)
            return;

        physicsMaterial.dynamicFriction = originPhysicsMaterial.dynamicFriction;
        physicsMaterial.staticFriction = originPhysicsMaterial.staticFriction;
        physicsMaterial.bounciness = originPhysicsMaterial.bounciness;
        physicsMaterial.frictionCombine = originPhysicsMaterial.frictionCombine;
        physicsMaterial.bounceCombine = originPhysicsMaterial.bounceCombine;
    }
}