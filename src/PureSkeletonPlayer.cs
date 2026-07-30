using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

public class PureSkeletonPlayer : MonoBehaviour
{
    [Header("JSON File")]
    public string jsonFileName = "S1_Directions_1_keypoints.json";

    [Header("Manual Skeleton")]
    public Transform manualSkeletonRoot;
    public string manualSkeletonName = "ManualSkeleton";

    [Header("Playback")]
    public float frameRate = 30f;
    public bool loop = true;
    public bool playOnStart = true;

    [Header("Fit Settings")]
    public bool autoFitScale = true;
    public float manualScale = 0.03f;
    public bool mirrorX = false;

    [Header("Bone Settings")]
    public float boneWidth = 0.12f;

    private List<Vector2[]> frames = new List<Vector2[]>();
    private int currentFrame = 0;
    private float timer = 0f;
    private bool isPlaying = false;

    private Dictionary<string, Transform> joints = new Dictionary<string, Transform>();
    private Dictionary<string, Transform> bones = new Dictionary<string, Transform>();

    private Vector2 referencePelvisData;
    private Vector3 referencePelvisUnity;
    private float appliedScale = 0.03f;

    private string[] jointNames = new string[]
    {
        "Pelvis",        // 0
        "LeftHip",       // 1
        "LeftKnee",      // 2
        "LeftAnkle",     // 3

        "RightHip",      // 4
        "RightKnee",     // 5
        "RightAnkle",    // 6

        "LowerTorso",    // 7
        "UpperTorso",    // 8
        "LowerNeck",     // 9
        "UpperNeck",     // 10

        "LeftShoulder",  // 11
        "LeftElbow",     // 12
        "LeftWrist",     // 13

        "RightShoulder", // 14
        "RightElbow",    // 15
        "RightWrist"     // 16
    };

    // 这里是“骨骼矩形”连接哪两个关节点
    private BoneBinding[] boneBindings = new BoneBinding[]
    {
        new BoneBinding("NeckLine", "LowerNeck", "UpperNeck"),

        new BoneBinding("LClavicleBone", "LowerNeck", "LeftShoulder"),
        new BoneBinding("RClavicleBone", "LowerNeck", "RightShoulder"),

        new BoneBinding("UpperSpineBone", "LowerNeck", "UpperTorso"),
        new BoneBinding("MiddleSpineBone", "UpperTorso", "LowerTorso"),
        new BoneBinding("LowerSpineBone", "LowerTorso", "Pelvis"),

        new BoneBinding("LUpperArm", "LeftShoulder", "LeftElbow"),
        new BoneBinding("LForeArm", "LeftElbow", "LeftWrist"),

        new BoneBinding("RUpperArm", "RightShoulder", "RightElbow"),
        new BoneBinding("RForeArm", "RightElbow", "RightWrist"),

        new BoneBinding("LHip", "Pelvis", "LeftHip"),
        new BoneBinding("RHip", "Pelvis", "RightHip"),

        new BoneBinding("LThigh", "LeftHip", "LeftKnee"),
        new BoneBinding("LLowerLeg", "LeftKnee", "LeftAnkle"),

        new BoneBinding("RThigh", "RightHip", "RightKnee"),
        new BoneBinding("RLowerLeg", "RightKnee", "RightAnkle")
    };

    void Start()
    {
        FindManualSkeleton();

        if (manualSkeletonRoot == null)
        {
            Debug.LogError("ManualSkeleton is missing. Please drag ManualSkeleton into the Manual Skeleton Root field.");
            return;
        }

        manualSkeletonRoot.gameObject.SetActive(true);

        CacheJoints();
        CacheBones();
        LoadJsonData();
        PrepareMapping();

        if (frames.Count > 0)
        {
            UpdatePose(frames[0]);
        }

        isPlaying = playOnStart;
    }

    void Update()
    {
        if (!isPlaying || frames.Count == 0)
        {
            return;
        }

        timer += Time.deltaTime;

        float frameInterval = 1f / Mathf.Max(1f, frameRate);

        if (timer >= frameInterval)
        {
            timer -= frameInterval;

            currentFrame++;

            if (currentFrame >= frames.Count)
            {
                if (loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = frames.Count - 1;
                    isPlaying = false;
                }
            }

            UpdatePose(frames[currentFrame]);
        }
    }

    private void FindManualSkeleton()
    {
        if (manualSkeletonRoot != null)
        {
            return;
        }

        GameObject found = GameObject.Find(manualSkeletonName);

        if (found != null)
        {
            manualSkeletonRoot = found.transform;
        }
    }

    private void CacheJoints()
    {
        joints.Clear();

        foreach (string jointName in jointNames)
        {
            Transform joint = FindDeepChild(manualSkeletonRoot, jointName);

            if (joint != null)
            {
                joints[jointName] = joint;
                joint.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Missing joint: " + jointName);
            }
        }

        Debug.Log("Cached joints: " + joints.Count);
    }

    private void CacheBones()
    {
        bones.Clear();

        foreach (BoneBinding binding in boneBindings)
        {
            Transform bone = FindDeepChild(manualSkeletonRoot, binding.boneName);

            if (bone != null)
            {
                bones[binding.boneName] = bone;
                bone.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Missing bone: " + binding.boneName);
            }
        }

        Debug.Log("Cached bones: " + bones.Count);
    }

    private void LoadJsonData()
    {
        frames.Clear();

        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName.Trim());

        Debug.Log("Trying to load JSON from: " + path);

        if (!File.Exists(path))
        {
            Debug.LogError("JSON file not found: " + path);
            return;
        }

        string text = File.ReadAllText(path);

        List<Vector2> allPairs = ParseVectorPairsOrTriples(text);

        if (allPairs.Count >= jointNames.Length)
        {
            BuildFramesFromPairs(allPairs);
            return;
        }

        List<float> allNumbers = ParseAllNumbers(text);

        if (allNumbers.Count >= jointNames.Length * 2)
        {
            BuildFramesFromFlatNumbers(allNumbers);
            return;
        }

        Debug.LogError(
            "No valid keypoint data found in JSON. " +
            "Pair count = " + allPairs.Count +
            ", number count = " + allNumbers.Count
        );
    }

    private List<Vector2> ParseVectorPairsOrTriples(string text)
    {
        List<Vector2> result = new List<Vector2>();

        string numberPattern = @"-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?";

        string pairOrTriplePattern =
            @"\[\s*(" + numberPattern + @")\s*,\s*(" + numberPattern + @")(?:\s*,\s*" + numberPattern + @")?\s*\]";

        MatchCollection matches = Regex.Matches(text, pairOrTriplePattern);

        foreach (Match match in matches)
        {
            float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

            result.Add(new Vector2(x, y));
        }

        Debug.Log("Parsed vector pairs/triples: " + result.Count);

        return result;
    }

    private List<float> ParseAllNumbers(string text)
    {
        List<float> result = new List<float>();

        string numberPattern = @"-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?";

        MatchCollection matches = Regex.Matches(text, numberPattern);

        foreach (Match match in matches)
        {
            float value = float.Parse(match.Value, CultureInfo.InvariantCulture);
            result.Add(value);
        }

        Debug.Log("Parsed flat numbers: " + result.Count);

        return result;
    }

    private void BuildFramesFromPairs(List<Vector2> allPairs)
    {
        int pointsPerFrame = jointNames.Length;
        int frameCount = allPairs.Count / pointsPerFrame;

        for (int f = 0; f < frameCount; f++)
        {
            Vector2[] frame = new Vector2[pointsPerFrame];

            for (int j = 0; j < pointsPerFrame; j++)
            {
                frame[j] = allPairs[f * pointsPerFrame + j];
            }

            frames.Add(frame);
        }

        Debug.Log("Loaded frames from vector pairs: " + frames.Count);
    }

    private void BuildFramesFromFlatNumbers(List<float> allNumbers)
    {
        int valuesPerFrame = jointNames.Length * 2;
        int frameCount = allNumbers.Count / valuesPerFrame;

        for (int f = 0; f < frameCount; f++)
        {
            Vector2[] frame = new Vector2[jointNames.Length];

            for (int j = 0; j < jointNames.Length; j++)
            {
                int index = f * valuesPerFrame + j * 2;

                float x = allNumbers[index];
                float y = allNumbers[index + 1];

                frame[j] = new Vector2(x, y);
            }

            frames.Add(frame);
        }

        Debug.Log("Loaded frames from flat numbers: " + frames.Count);
    }

    private void PrepareMapping()
    {
        if (frames.Count == 0)
        {
            Debug.LogError("No frames loaded. Cannot prepare mapping.");
            return;
        }

        if (!joints.ContainsKey("Pelvis"))
        {
            Debug.LogError("Pelvis joint is missing. Cannot prepare mapping.");
            return;
        }

        referencePelvisData = frames[0][0];
        referencePelvisUnity = joints["Pelvis"].localPosition;

        if (autoFitScale && joints.ContainsKey("UpperNeck"))
        {
            float unityHeight = Mathf.Abs(joints["UpperNeck"].localPosition.y - joints["Pelvis"].localPosition.y);
            float dataHeight = Mathf.Abs(frames[0][10].y - frames[0][0].y);

            if (dataHeight > 0.001f)
            {
                appliedScale = unityHeight / dataHeight;
            }
            else
            {
                appliedScale = manualScale;
            }
        }
        else
        {
            appliedScale = manualScale;
        }

        Debug.Log("Applied scale: " + appliedScale);
    }

    private void UpdatePose(Vector2[] frame)
    {
        UpdateJoints(frame);
        UpdateBones();
    }

    private void UpdateJoints(Vector2[] frame)
    {
        for (int i = 0; i < jointNames.Length; i++)
        {
            string jointName = jointNames[i];

            if (!joints.ContainsKey(jointName))
            {
                continue;
            }

            Vector2 dataPoint = frame[i];

            float xOffset = dataPoint.x - referencePelvisData.x;
            float yOffset = dataPoint.y - referencePelvisData.y;

            if (mirrorX)
            {
                xOffset = -xOffset;
            }

            float unityX = referencePelvisUnity.x + xOffset * appliedScale;

            float unityY = referencePelvisUnity.y - yOffset * appliedScale;

            Transform joint = joints[jointName];
            joint.localPosition = new Vector3(unityX, unityY, joint.localPosition.z);
        }
    }

    private void UpdateBones()
    {
        foreach (BoneBinding binding in boneBindings)
        {
            if (!bones.ContainsKey(binding.boneName))
            {
                continue;
            }

            if (!joints.ContainsKey(binding.startJoint) || !joints.ContainsKey(binding.endJoint))
            {
                continue;
            }

            Transform bone = bones[binding.boneName];
            Transform start = joints[binding.startJoint];
            Transform end = joints[binding.endJoint];

            Vector3 startPos = start.localPosition;
            Vector3 endPos = end.localPosition;

            Vector3 middle = (startPos + endPos) / 2f;
            Vector3 direction = endPos - startPos;

            float length = direction.magnitude;

            if (length < 0.001f)
            {
                continue;
            }

            bone.localPosition = middle;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            bone.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);

            bone.localScale = new Vector3(boneWidth, length, bone.localScale.z);
        }
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform result = FindDeepChild(child, childName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private class BoneBinding
    {
        public string boneName;
        public string startJoint;
        public string endJoint;

        public BoneBinding(string boneName, string startJoint, string endJoint)
        {
            this.boneName = boneName;
            this.startJoint = startJoint;
            this.endJoint = endJoint;
        }
    }
}