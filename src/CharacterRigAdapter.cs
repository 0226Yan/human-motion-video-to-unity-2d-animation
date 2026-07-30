using UnityEngine;

public class CharacterRigAdapter : MonoBehaviour
{
    [Header("Source Skeleton Joints (17 Keypoints)")]
    public Transform pelvis;        // 0
    public Transform leftHip;       // 1
    public Transform leftKnee;      // 2
    public Transform leftAnkle;     // 3
    public Transform rightHip;      // 4
    public Transform rightKnee;     // 5
    public Transform rightAnkle;    // 6
    public Transform lowerTorso;    // 7
    public Transform upperTorso;    // 8
    public Transform lowerNeck;     // 9
    public Transform upperNeck;     // 10
    public Transform leftShoulder;  // 11
    public Transform leftElbow;     // 12
    public Transform leftWrist;     // 13
    public Transform rightShoulder; // 14
    public Transform rightElbow;    // 15
    public Transform rightWrist;    // 16

    [Header("Character Root")]
    public Transform characterRoot;

    [Header("Core Character Parts")]
    public Transform headPart;      // Head_Pivot
    public Transform neckPart;      // Neck_Pivot
    public Transform torsoPart;     // Body_Visual or Body_Group

    [Header("Spine Parts")]
    public Transform upperSpinePart;
    public Transform middleSpinePart;
    public Transform lowerSpinePart;

    [Header("Shoulder Parts")]
    public Transform leftClaviclePart;
    public Transform rightClaviclePart;

    [Header("Arm Parts")]
    public Transform leftUpperArmPart;
    public Transform leftForeArmPart;
    public Transform rightUpperArmPart;
    public Transform rightForeArmPart;

    [Header("Hip Parts")]
    public Transform leftHipPart;
    public Transform rightHipPart;

    [Header("Leg Parts")]
    public Transform leftThighPart;
    public Transform leftLowerLegPart;
    public Transform rightThighPart;
    public Transform rightLowerLegPart;

    [Header("Motion Settings")]
    [InspectorName("Move Whole Character With Skeleton")]
    public bool moveWholeCharacterWithSkeleton = false;

    [InspectorName("Rotate Body With Skeleton")]
    public bool rotateBodyWithSkeleton = false;

    [InspectorName("Swap Left / Right Mapping")]
    public bool swapLeftRightMapping = false;

    [Header("Head Rotation Limit")]
    public bool limitHeadRotation = true;
    [Range(0f, 1f)] public float headRotationStrength = 0.18f;
    public float maxHeadAngle = 15f;

    private const bool rotateOtherParts = true;
    private const bool keepInitialRotationOffset = true;
    private const bool captureOnStart = true;

    private bool referenceReady = false;
    private bool lastSwapLeftRightMapping = false;

    private PartState headState;
    private PartState neckState;
    private PartState torsoState;

    private PartState upperSpineState;
    private PartState middleSpineState;
    private PartState lowerSpineState;

    private PartState leftClavicleState;
    private PartState rightClavicleState;

    private PartState leftUpperArmState;
    private PartState leftForeArmState;
    private PartState rightUpperArmState;
    private PartState rightForeArmState;

    private PartState leftHipState;
    private PartState rightHipState;

    private PartState leftThighState;
    private PartState leftLowerLegState;
    private PartState rightThighState;
    private PartState rightLowerLegState;

    private class PartState
    {
        public bool valid;
        public Vector3 positionOffsetFromBoneCenter;
        public Quaternion rotationOffset;
        public float initialBoneAngle;
        public float initialPartAngle;
    }

    void Start()
    {
        if (captureOnStart)
        {
            CaptureReferencePose();
        }
    }

    void LateUpdate()
    {
        if (!referenceReady)
        {
            return;
        }

        if (lastSwapLeftRightMapping != swapLeftRightMapping)
        {
            CaptureReferencePose();
        }

        Transform sourceLeftHip = MapLeft(leftHip, rightHip);
        Transform sourceLeftKnee = MapLeft(leftKnee, rightKnee);
        Transform sourceLeftAnkle = MapLeft(leftAnkle, rightAnkle);
        Transform sourceLeftShoulder = MapLeft(leftShoulder, rightShoulder);
        Transform sourceLeftElbow = MapLeft(leftElbow, rightElbow);
        Transform sourceLeftWrist = MapLeft(leftWrist, rightWrist);

        Transform sourceRightHip = MapRight(leftHip, rightHip);
        Transform sourceRightKnee = MapRight(leftKnee, rightKnee);
        Transform sourceRightAnkle = MapRight(leftAnkle, rightAnkle);
        Transform sourceRightShoulder = MapRight(leftShoulder, rightShoulder);
        Transform sourceRightElbow = MapRight(leftElbow, rightElbow);
        Transform sourceRightWrist = MapRight(leftWrist, rightWrist);

        ApplyTorso(torsoPart, pelvis, upperTorso, torsoState);

        if (!rotateOtherParts)
        {
            return;
        }

        if (limitHeadRotation)
        {
            ApplyLimitedRotationOnly(headPart, lowerNeck, upperNeck, headState, headRotationStrength, maxHeadAngle);
        }
        else
        {
            ApplyRotationOnly(headPart, lowerNeck, upperNeck, headState);
        }

        ApplyRotationOnly(neckPart, upperTorso, lowerNeck, neckState);

        ApplyRotationOnly(upperSpinePart, upperTorso, lowerNeck, upperSpineState);
        ApplyRotationOnly(middleSpinePart, lowerTorso, upperTorso, middleSpineState);
        ApplyRotationOnly(lowerSpinePart, pelvis, lowerTorso, lowerSpineState);

        ApplyRotationOnly(leftClaviclePart, lowerNeck, sourceLeftShoulder, leftClavicleState);
        ApplyRotationOnly(rightClaviclePart, lowerNeck, sourceRightShoulder, rightClavicleState);

        ApplyRotationOnly(leftUpperArmPart, sourceLeftShoulder, sourceLeftElbow, leftUpperArmState);
        ApplyRotationOnly(leftForeArmPart, sourceLeftElbow, sourceLeftWrist, leftForeArmState);
        ApplyRotationOnly(rightUpperArmPart, sourceRightShoulder, sourceRightElbow, rightUpperArmState);
        ApplyRotationOnly(rightForeArmPart, sourceRightElbow, sourceRightWrist, rightForeArmState);

        ApplyRotationOnly(leftHipPart, pelvis, sourceLeftHip, leftHipState);
        ApplyRotationOnly(rightHipPart, pelvis, sourceRightHip, rightHipState);

        ApplyRotationOnly(leftThighPart, sourceLeftHip, sourceLeftKnee, leftThighState);
        ApplyRotationOnly(leftLowerLegPart, sourceLeftKnee, sourceLeftAnkle, leftLowerLegState);
        ApplyRotationOnly(rightThighPart, sourceRightHip, sourceRightKnee, rightThighState);
        ApplyRotationOnly(rightLowerLegPart, sourceRightKnee, sourceRightAnkle, rightLowerLegState);
    }

    [ContextMenu("Capture Reference Pose")]
    public void CaptureReferencePose()
    {
        Transform sourceLeftHip = MapLeft(leftHip, rightHip);
        Transform sourceLeftKnee = MapLeft(leftKnee, rightKnee);
        Transform sourceLeftAnkle = MapLeft(leftAnkle, rightAnkle);
        Transform sourceLeftShoulder = MapLeft(leftShoulder, rightShoulder);
        Transform sourceLeftElbow = MapLeft(leftElbow, rightElbow);
        Transform sourceLeftWrist = MapLeft(leftWrist, rightWrist);

        Transform sourceRightHip = MapRight(leftHip, rightHip);
        Transform sourceRightKnee = MapRight(leftKnee, rightKnee);
        Transform sourceRightAnkle = MapRight(leftAnkle, rightAnkle);
        Transform sourceRightShoulder = MapRight(leftShoulder, rightShoulder);
        Transform sourceRightElbow = MapRight(leftElbow, rightElbow);
        Transform sourceRightWrist = MapRight(leftWrist, rightWrist);

        headState = CapturePart(headPart, lowerNeck, upperNeck);
        neckState = CapturePart(neckPart, upperTorso, lowerNeck);
        torsoState = CapturePart(torsoPart, pelvis, upperTorso);

        upperSpineState = CapturePart(upperSpinePart, upperTorso, lowerNeck);
        middleSpineState = CapturePart(middleSpinePart, lowerTorso, upperTorso);
        lowerSpineState = CapturePart(lowerSpinePart, pelvis, lowerTorso);

        leftClavicleState = CapturePart(leftClaviclePart, lowerNeck, sourceLeftShoulder);
        rightClavicleState = CapturePart(rightClaviclePart, lowerNeck, sourceRightShoulder);

        leftUpperArmState = CapturePart(leftUpperArmPart, sourceLeftShoulder, sourceLeftElbow);
        leftForeArmState = CapturePart(leftForeArmPart, sourceLeftElbow, sourceLeftWrist);
        rightUpperArmState = CapturePart(rightUpperArmPart, sourceRightShoulder, sourceRightElbow);
        rightForeArmState = CapturePart(rightForeArmPart, sourceRightElbow, sourceRightWrist);

        leftHipState = CapturePart(leftHipPart, pelvis, sourceLeftHip);
        rightHipState = CapturePart(rightHipPart, pelvis, sourceRightHip);

        leftThighState = CapturePart(leftThighPart, sourceLeftHip, sourceLeftKnee);
        leftLowerLegState = CapturePart(leftLowerLegPart, sourceLeftKnee, sourceLeftAnkle);
        rightThighState = CapturePart(rightThighPart, sourceRightHip, sourceRightKnee);
        rightLowerLegState = CapturePart(rightLowerLegPart, sourceRightKnee, sourceRightAnkle);

        lastSwapLeftRightMapping = swapLeftRightMapping;
        referenceReady = true;
    }

    private PartState CapturePart(Transform part, Transform jointA, Transform jointB)
    {
        PartState state = new PartState();

        if (part == null || jointA == null || jointB == null)
        {
            state.valid = false;
            return state;
        }

        Vector3 boneCenter = GetBoneCenter(jointA, jointB);
        Quaternion boneRotation = GetBoneRotation(jointA, jointB);

        state.positionOffsetFromBoneCenter = part.position - boneCenter;
        state.initialBoneAngle = GetBoneAngle2D(jointA, jointB);
        state.initialPartAngle = NormalizeAngle(part.eulerAngles.z);

        if (keepInitialRotationOffset)
        {
            state.rotationOffset = Quaternion.Inverse(boneRotation) * part.rotation;
        }
        else
        {
            state.rotationOffset = Quaternion.identity;
        }

        state.valid = true;
        return state;
    }

    private void ApplyTorso(Transform part, Transform jointA, Transform jointB, PartState state)
    {
        if (part == null || jointA == null || jointB == null || state == null || !state.valid)
        {
            return;
        }

        Vector3 boneCenter = GetBoneCenter(jointA, jointB);
        Quaternion boneRotation = GetBoneRotation(jointA, jointB);

        if (moveWholeCharacterWithSkeleton)
        {
            Vector3 targetPartPosition = boneCenter + state.positionOffsetFromBoneCenter;

            if (characterRoot != null && characterRoot != part)
            {
                Vector3 moveDelta = targetPartPosition - part.position;
                characterRoot.position += moveDelta;
            }
            else
            {
                part.position = targetPartPosition;
            }
        }

        if (rotateBodyWithSkeleton)
        {
            ApplyRotation(part, boneRotation * state.rotationOffset);
        }
    }

    private void ApplyRotationOnly(Transform part, Transform jointA, Transform jointB, PartState state)
    {
        if (part == null || jointA == null || jointB == null || state == null || !state.valid)
        {
            return;
        }

        Quaternion boneRotation = GetBoneRotation(jointA, jointB);
        Quaternion targetRotation = boneRotation * state.rotationOffset;

        ApplyRotation(part, targetRotation);
    }

    private void ApplyLimitedRotationOnly(
        Transform part,
        Transform jointA,
        Transform jointB,
        PartState state,
        float strength,
        float maxAngle
    )
    {
        if (part == null || jointA == null || jointB == null || state == null || !state.valid)
        {
            return;
        }

        float currentBoneAngle = GetBoneAngle2D(jointA, jointB);
        float boneDelta = Mathf.DeltaAngle(state.initialBoneAngle, currentBoneAngle);
        float limitedDelta = Mathf.Clamp(boneDelta * strength, -maxAngle, maxAngle);
        float targetAngle = state.initialPartAngle + limitedDelta;

        ApplyRotation(part, Quaternion.Euler(0f, 0f, targetAngle));
    }

    private void ApplyRotation(Transform part, Quaternion targetRotation)
    {
        part.rotation = targetRotation;
    }

    private Transform MapLeft(Transform left, Transform right)
    {
        if (swapLeftRightMapping)
        {
            return right;
        }

        return left;
    }

    private Transform MapRight(Transform left, Transform right)
    {
        if (swapLeftRightMapping)
        {
            return left;
        }

        return right;
    }

    private Vector3 GetBoneCenter(Transform jointA, Transform jointB)
    {
        return (jointA.position + jointB.position) / 2f;
    }

    private Quaternion GetBoneRotation(Transform jointA, Transform jointB)
    {
        return Quaternion.Euler(0f, 0f, GetBoneAngle2D(jointA, jointB));
    }

    private float GetBoneAngle2D(Transform from, Transform to)
    {
        Vector3 direction = to.position - from.position;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return 0f;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        return angle - 90f;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        while (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }
}