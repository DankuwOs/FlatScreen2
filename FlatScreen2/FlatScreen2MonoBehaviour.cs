
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using VTOLVR.Multiplayer;

using Triquetra;
using Triquetra.FlatScreen.TrackIR;

namespace muskit.FlatScreen2
{
    public class FlatScreen2MonoBehaviour : MonoBehaviour
    {
        // https://vtolvr-mods.com/viewbugs/zj7ylyrf/
        // TODO: Usable with VR headset active (disable VR to control cameras)
            // - Be able to start flight without putting helmet on
        // TODO?: WASDEQ controls
        // TODO?: Bobblehead gets a VRInteractable

        public static FlatScreen2MonoBehaviour instance { get; private set; }


        private Rect windowRect = new Rect(25, 25, 350, 500);
        private bool showWindow = true;
        public TrackIRTransformer TrackIRTransformer { get; private set; }

        public bool flatScreenEnabled { get; private set; } = false;

        public VRInteractable targetedVRInteractable;
        public VRInteractable heldVRInteractable;
        public IEnumerable<VRInteractable> VRInteractables = new List<VRInteractable>();

        private Rect endMissionWindowRect = new Rect(Screen.width / 2 - 300, Screen.height / 2 - 400, 600, 800);
        private bool showEndMissionWindow = false;
        private bool endMissionWindowAutoShown = false;
        EndMission endMission;

        private bool viewIsSpec = false;

        // player avatar
        private GameObject playerBody = null;
        private GameObject playerLeftHand = null;
        private GameObject playerRightHand = null;

        public static bool IsFlyingScene()
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            return buildIndex == 7 || buildIndex == 11;
        }

        public static bool IsReadyRoomScene()
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            return buildIndex == 2;
        }

        public static Camera GetEyeCamera()
        {
            IEnumerable<Camera> cameras = GameObject.FindObjectsOfType<Camera>(false)
                .Where(c => c.name == "Camera (eye)" && c.isActiveAndEnabled)
                .OrderByDescending(c => c.depth);

            if (cameras.Any(x => x.gameObject?.layer == LayerMask.NameToLayer("MPBriefing")))
            {
                if (VTOLMPSceneManager.instance.localPlayer.chosenTeam)
                {
                    GameObject localAvatarObject = typeof(VTOLMPSceneManager)
                        .GetField("localAvatarObj", BindingFlags.Instance | BindingFlags.NonPublic)?
                        .GetValue(VTOLMPSceneManager.instance) as GameObject;
                    if (localAvatarObject != null)
                    {
                        Camera localAvatarCam = localAvatarObject?.GetComponentInChildren<Camera>(false);
                        if (localAvatarCam != null)
                        {
                            return localAvatarCam;
                        }
                    }
                }
            }
            //FlatScreen2Plugin.Write($"finished populating eye camera list ({cameras.Count()})");
            var cam = cameras.FirstOrDefault();
            //FlatScreen2Plugin.Write($"using camera {cam?.name}");
            return cam;
        }

        public static IEnumerable<Camera> GetSpectatorCameras()
        {
            return GameObject.FindObjectsOfType<Camera>(true).Where(c => c.name == "FlybyCam" || c.name == "flybyHMCScam");
        }

        public FlatScreen2MonoBehaviour()
        {
            if (instance != null)
            {
                FlatScreen2Plugin.Write("WARNING: Tried to create another MonoBehaviour instance when one already exists! Destroying self.");
                Destroy(this);
            }
            else
                instance = this;
        }

        public void Activate()
        {
            VRHead.OnVRHeadChanged += ResetState;

            VRUtils.DisableVR();
            ResetState();

            // TODO: set camera parameters to look less warped
        }

        public void ResetState()
        {
            FlatScreen2Plugin.Write("State reset!");
            showEndMissionWindow = false;
            endMissionWindowAutoShown = false;

            currentFOV = DEFAULT_FOV;
            RegrabTracks();
        }

        public void OnGUI()
        {
            if (showWindow)
                windowRect = GUI.Window(405, windowRect, GUIMainWindow, "FlatScreen 2 Control Panel");
            if (showEndMissionWindow)
                endMissionWindowRect = GUI.Window(406, endMissionWindowRect, GUIEndMissionWindow, "FlatScreen 2 End Mission");
        }

        public void ToggleEndMissionWindow()
        {
            showEndMissionWindow = !showEndMissionWindow;
            if (showEndMissionWindow)
                GUI.FocusWindow(406);
        }

        private void GUIEndMissionWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            // GUILayout.Label($"Completion Time: {endMission.metCompleteText?.text}");

            if (GUILayout.Button("Restart Mission"))
            {
                endMission?.ReloadSceneButton();
                showEndMissionWindow = false;
            }
            if (GUILayout.Button("Finish Mission"))
            {
                endMission?.ReturnToMainButton();
                showEndMissionWindow = false;
            }

            // flight log
            var stringBuilder = new System.Text.StringBuilder();
            foreach (FlightLogger.LogEntry logEntry in FlightLogger.GetLog())
                stringBuilder.AppendLine(logEntry.timestampedMessage);
            GUI.TextArea(new Rect(25, 80, 550, 600), stringBuilder.ToString());

            GUILayout.Space(650);

            if (QuicksaveManager.quickloadAvailable && GUILayout.Button("Load Quicksave"))
            {
                endMission?.Quickload();
                showEndMissionWindow = false;
            }
        }

        private Vector2 mainWinScrollPos;
        void GUIMainWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.Label("Press F9 to show/hide this window");
            GUILayout.Space(20);

            if (!flatScreenEnabled)
            {
                bool toEnable = GUILayout.Button("Activate (cannot revert!!)");
                if (toEnable)
                {
                    flatScreenEnabled = true;
                    Activate();
                }

                return;
            }

            mainWinScrollPos = GUILayout.BeginScrollView(mainWinScrollPos);
            {
                if (cameraEyeGameObject != null || TryUpdateCameraTracks())
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label($"FOV: {currentFOV}");
                        int newFOV = (int)Mathf.Round(GUILayout.HorizontalSlider(currentFOV, 30f, 120f));

                        if (newFOV != currentFOV)
                            SetCameraFOV(newFOV);
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Mouse Sensitivity: {Sensitivity}");
                Sensitivity = Mathf.Round(GUILayout.HorizontalSlider(Sensitivity, 1f, 9f));
                GUILayout.EndHorizontal();

                LimitXRotation = GUILayout.Toggle(LimitXRotation, " Limit X Rotation");
                LimitYRotation = GUILayout.Toggle(LimitYRotation, " Limit Y Rotation");

                if (GUILayout.Button("Reset Camera Rotation"))
                    ResetCameraRotation();

                GUILayout.Space(30);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"Hovered VRInteractable:");
                    if (targetedVRInteractable != null)
                        GUILayout.Label(targetedVRInteractable?.interactableName ?? "???");
                    else
                        GUILayout.Label("[None]");
                }
                GUILayout.EndHorizontal();

                if (targetedVRInteractable != null)
                    foreach (var comp in targetedVRInteractable?.GetComponents<MonoBehaviour>())
                    {
                        GUILayout.Label(comp.GetType().Name);
                    }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"Held-Down VRInteractable:");
                    if (heldVRInteractable != null)
                        GUILayout.Label(heldVRInteractable?.interactableName ?? "???");
                    else
                        GUILayout.Label("[None]");
                }
                GUILayout.EndHorizontal();

                GUILayout.Label("Use the scroll wheel on non-integer knobs");

                GUILayout.Space(20);

                if (GUILayout.Button("Reset Camera"))
                {
                    RegrabTracks();
                }

                if (GUILayout.Button("View: " + (viewIsSpec ? "S-CAM" : "First Person")))
                {
                    viewIsSpec = !viewIsSpec;

                    foreach (Camera specCam in GetSpectatorCameras())
                    {
                        specCam.depth = viewIsSpec ? 50 : -6;
                    }

                    SetAvatarVisibility(viewIsSpec);
                }

                GUI.enabled = true;

                /*if (IsReadyRoomScene())
                {
                    if (GUILayout.Button("Quick Select Vehicle"))
                    {
                        PilotSelectUI pilotSelectUI = FindObjectOfType<PilotSelectUI>();
                        pilotSelectUI.StartSelectedPilotButton();
                        pilotSelectUI.SelectVehicleButton();
                    }
                }*/

                GUILayout.Space(30);

                if (GUILayout.Button("Start Tracking"))
                {
                    if (TrackIRTransformer == null)
                        TrackIRTransformer = GetComponent<TrackIRTransformer>() ?? gameObject.AddComponent<TrackIRTransformer>();
                    TrackIRTransformer.StartTracking();
                }
                if (GUILayout.Button("Stop Tracking"))
                {
                    if (TrackIRTransformer == null)
                        TrackIRTransformer = GetComponent<TrackIRTransformer>() ?? gameObject.AddComponent<TrackIRTransformer>();
                    TrackIRTransformer.StopTracking();
                }

                GUILayout.Space(30);
                /*
                Camera camera = GetEyeCamera();
                if (camera != null)
                {
                    GUILayout.Label($"Camera: {camera.name}");
                    GUILayout.Label($"Camera GameObject: {GetEyeCameraGameObject()?.name}");
                    GUILayout.Label($"Depth: {camera.depth}");
                    GUILayout.Label($"Enabled: {camera.enabled}");
                    GUILayout.Label($"Is Active and Enabled: {camera.isActiveAndEnabled}");
                    GUILayout.Label($"Quad Parent: {camera.transform.parent?.parent?.parent?.parent?.name}");
                }

                GUILayout.Space(30);

                if (targetedVRInteractable != null)
                {
                    VRThrottle throttle = targetedVRInteractable.GetComponent<VRThrottle>();
                    GUILayout.Label($"Throttle: {throttle}");
                    GUILayout.Label($"Throttle Transform: {throttle?.throttleTransform?.name}");
                }*/
            }
            GUILayout.EndScrollView();
        }

        public void SetTrackingObject(GameObject trackingObject)
        {
            if (TrackIRTransformer == null)
                TrackIRTransformer = GetComponent<TrackIRTransformer>() ?? gameObject.AddComponent<TrackIRTransformer>();

            if (TrackIRTransformer == null)
                return;

            TrackIRTransformer.trackedObject = trackingObject?.transform;
        }

        public void SetAvatarVisibility(bool isVis)
        {
            /* The scene paths here will likely need to be updated if a game update changes any of these. */
            if (playerBody == null)
                playerBody = GameObject.Find("suit2/RiggedSuit.001");
            if (playerLeftHand == null)
                playerLeftHand = GameObject.Find("Controller (left)/newGlove/SWAT_glower_pivot.002");
            if (playerRightHand == null)
                playerRightHand = GameObject.Find("Controller (right)/newGlove/SWAT_glower_pivot.002");

            // helmet if in team select
            var teamSelectAv = GameObject.Find("TeamSelectSpawn/BriefingAvatar");
            if (teamSelectAv != null)
            {
                teamSelectAv.transform
                    .Find("Local/CameraRigParent/[CameraRig]/Camera (eye)/Helmet/hqh")?
                    .gameObject.SetActive(isVis);
            }

            // helmet if in MP briefing room
            var seshPms = FindObjectsOfType<PlayerModelSync>();
            foreach (var pms in seshPms)
            {
                if (pms.isMine && pms.gameObject.name.Contains("BriefingAvatar"))
                {
                    pms.transform
                    .Find("Local/CameraRigParent/[CameraRig]/Camera (eye)/Helmet/hqh")
                    .gameObject.SetActive(isVis);
                    break;
                }
            }
            
            // body visiblity
            FlatScreen2Plugin.Write($"Setting body ({playerBody}) vis to {isVis}");
            playerBody?.SetActive(isVis);

            // hands visibility
            FlatScreen2Plugin.Write($"Setting left hand ({playerLeftHand}) vis to {isVis}");
            playerLeftHand?.SetActive(isVis);
            FlatScreen2Plugin.Write($"Setting right hand ({playerRightHand}) vis to {isVis}");
            playerRightHand?.SetActive(isVis);
        }

        public float Sensitivity = 2f;
        public float RotationLimitX = 160f; // set to -1 to disable
        public float RotationLimitY = 89f; // set to -1 to disable

        public bool LimitXRotation
        {
            get { return RotationLimitX >= 0; }
            set { RotationLimitX = value ? 160f : -1f; }
        }
        public bool LimitYRotation
        {
            get { return RotationLimitY >= 0; }
            set { RotationLimitY = value ? 89f : -1f; }
        }

        public GameObject cameraEyeGameObject;
        public GameObject cameraHMDGameObject;
        public GameObject cameraHelmetGameObject;

        public const int DEFAULT_FOV = 60;
        int currentFOV = DEFAULT_FOV;
        private Vector2 cameraRotation = Vector2.zero;
        private const string xAxis = "Mouse X";
        private const string yAxis = "Mouse Y";

        public void MoveCamera()
        {
            if (Input.GetMouseButton(1))
            {
                cameraRotation.x += Input.GetAxis(xAxis) * Sensitivity;
                cameraRotation.y += Input.GetAxis(yAxis) * Sensitivity;
                if (RotationLimitX > 0)
                    cameraRotation.x = Mathf.Clamp(cameraRotation.x, -RotationLimitX, RotationLimitX);
                if (RotationLimitY > 0)
                    cameraRotation.y = Mathf.Clamp(cameraRotation.y, -RotationLimitY, RotationLimitY);
                var xQuat = Quaternion.AngleAxis(cameraRotation.x, Vector3.up);
                var yQuat = Quaternion.AngleAxis(cameraRotation.y, Vector3.left);

                cameraEyeGameObject.transform.localRotation = xQuat * yQuat; //Quaternions seem to rotate more consistently than EulerAngles. Sensitivity seemed to change slightly at certain degrees using Euler.
                                                                                //transform.localEulerAngles = new Vector3(-rotation.y, rotation.x, 0);
            }
        }
        
        public void ResetCameraRotation()
        {
            cameraEyeGameObject.transform.localRotation = Quaternion.identity;
            cameraRotation = Vector2.zero;
        }

        public void SetCameraFOV(int fov)
        {
            fov = Mathf.Clamp(fov, 30, 120);
            currentFOV = fov;

            if (cameraEyeGameObject != null)
                cameraEyeGameObject.GetComponent<Camera>().fieldOfView = fov;
            if (cameraHMDGameObject != null)
                cameraHMDGameObject.GetComponent<Camera>().fieldOfView = fov;
        }
        public float GetCameraFOV()
        {
            return cameraEyeGameObject.GetComponent<Camera>().fieldOfView;
        }

        /// <summary>
        /// Update this instance's camera variables.
        /// </summary>
        /// <returns>If the main camera was grabbed successfully.</returns>
        public bool TryUpdateCameraTracks()
        {
            if (cameraEyeGameObject == null)
            {
                cameraEyeGameObject = GetEyeCamera()?.gameObject;

                if (cameraEyeGameObject != null)
                {
                    cameraHMDGameObject = cameraEyeGameObject.transform.Find("Camera HMD HUD")?.gameObject;
                    cameraHelmetGameObject = cameraEyeGameObject.transform.Find("Camera (eye) Helmet")?.gameObject;
                }

                if (cameraHelmetGameObject != null)
                    cameraHelmetGameObject.GetComponent<Camera>().fieldOfView = 20;
            }

            return cameraEyeGameObject != null;
        }

        public void RegrabTracks()
        {
            FlatScreen2Plugin.Write("Regrabbing tracked player objects...");
            cameraEyeGameObject = null;
            cameraHMDGameObject = null;
            playerBody = null;
            playerLeftHand = null;
            playerRightHand = null;
            viewIsSpec = false;

            if (TryUpdateCameraTracks())
            {
                FlatScreen2Plugin.Write($"    Camera grabbed: {cameraEyeGameObject}");
                ResetCameraRotation();
                SetCameraFOV(currentFOV);
            }
            else
            {
                FlatScreen2Plugin.Write($"    Could not find camera!");
            }

            foreach (Camera specCam in GetSpectatorCameras())
            {
                FlatScreen2Plugin.Write($"    SpecCam ({specCam.name}) depth: {specCam.depth}");
                specCam.depth = -6;
            }

            SetAvatarVisibility(false);
        }

        public void GetHoveredObject()
        {
            // Logger.WriteLine($"Checking intersected VRInteractables");

            Camera camera = cameraEyeGameObject.GetComponent<Camera>();

            Ray ray = camera.ScreenPointToRay(Input.mousePosition, Camera.MonoOrStereoscopicEye.Mono);

            List<VRInteractable> intersectedInteractables = new List<VRInteractable>();

            foreach (VRInteractable interactable in VRInteractables)
            {
                if (interactable == null || interactable.transform == null)
                    continue;

                Bounds bounds;

                float radius = Mathf.Min(Mathf.Max(0.01f, interactable.radius), 0.1f); // have a minimum (and maximum) radius to avoid 0 size radius (and to avoid having to calculate rect sizes)
                bounds = new Bounds(interactable.transform.position, Vector3.one * radius);

                if (bounds.IntersectRay(ray))
                {
                    intersectedInteractables.Add(interactable);
                }
            }

            float depth = 0.5f;
            VRInteractable hoveredInteractable = intersectedInteractables
                .Where(x => x != null && x.transform != null)
                .OrderBy((x) => Vector3.Distance(x.transform.position, ray.origin + (ray.direction * depth)))
                .FirstOrDefault();

            targetedVRInteractable = hoveredInteractable;
        }

        MeshRenderer PreviouslyHighlightedInteractableRenderer;
        Dictionary<MeshRenderer, Color> VRInteractableOriginalColors = new Dictionary<MeshRenderer, Color>();
        private void HighlightObject(VRInteractable targetedVRInteractable)
        {
            HighlightImage(targetedVRInteractable);

            if (PreviouslyHighlightedInteractableRenderer != null)
            {
                PreviouslyHighlightedInteractableRenderer.material.color = VRInteractableOriginalColors[PreviouslyHighlightedInteractableRenderer];
                PreviouslyHighlightedInteractableRenderer = null;
            }

            if (targetedVRInteractable == null)
                return;

            MeshRenderer renderer = GetMeshRendererFromVRInteractable(targetedVRInteractable);
            if (renderer == null)
            {
                return;
            }

            VRInteractableOriginalColors[renderer] = renderer.material.color;

            if (renderer != null)
                renderer.material.color = Color.yellow;

            if (targetedVRInteractable != null)
                PreviouslyHighlightedInteractableRenderer = renderer;
        }

        Image PreviouslyHighlightedInteractableImage;
        Dictionary<Image, Color> VRInteractableImageOriginalColors = new Dictionary<Image, Color>();
        private void HighlightImage(VRInteractable targetedVRInteractable)
        {
            if (PreviouslyHighlightedInteractableImage != null)
            {
                PreviouslyHighlightedInteractableImage.color = VRInteractableImageOriginalColors[PreviouslyHighlightedInteractableImage];
                PreviouslyHighlightedInteractableImage = null;
            }

            if (targetedVRInteractable == null)
                return;

            Image image = GetImageFromVRInteractable(targetedVRInteractable);
            if (image == null)
            {
                return;
            }

            VRInteractableImageOriginalColors[image] = image.color;

            if (image != null)
                image.color = Color.yellow;

            if (targetedVRInteractable != null)
                PreviouslyHighlightedInteractableImage = image;
        }

        private MeshRenderer GetMeshRendererFromVRInteractable(VRInteractable interactable)
        {
            VRButton button = interactable.GetComponent<VRButton>();
            VRLever lever = interactable.GetComponent<VRLever>();
            VRTwistKnob twistKnob = interactable.GetComponent<VRTwistKnob>();
            VRTwistKnobInt twistKnobInt = interactable.GetComponent<VRTwistKnobInt>();
            MeshRenderer meshRenderer = interactable.GetComponent<MeshRenderer>();
            MeshRenderer childMeshRenderer = interactable.GetComponentInChildren<MeshRenderer>();

            if (meshRenderer != null)
                return meshRenderer;
            if (childMeshRenderer != null)
                return childMeshRenderer;

            Transform transform = button?.buttonTransform ??
                lever?.leverTransform ??
                twistKnob?.knobTransform ??
                twistKnobInt?.knobTransform;

            return transform?.GetComponent<MeshRenderer>() ??
                transform?.parent?.GetComponent<MeshRenderer>() ??
                transform?.parent?.parent?.GetComponent<MeshRenderer>() ??
                transform?.GetComponentInChildren<MeshRenderer>();
        }

        private Image GetImageFromVRInteractable(VRInteractable interactable)
        {
            Image image = interactable.GetComponent<Image>();
            Image childImage = interactable.GetComponentInChildren<Image>();
            Image parentImage = interactable.GetComponentInParent<Image>();
            Image parentParentImage = interactable.transform.parent?.GetComponentInParent<Image>();

            return image ??
                childImage ??
                parentImage ??
                parentParentImage;
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
                showWindow = !showWindow;

            if (!flatScreenEnabled)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                ToggleEndMissionWindow();

            if (cameraEyeGameObject == null)
                return;

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
                ResetCameraRotation();

            HighlightObject(targetedVRInteractable);

            // TODO: change to subtle cursor location indication
            Cursor.visible = !Input.GetMouseButton(1);

            if (Input.GetMouseButtonDown(0)) // left mouse down
            {
                if (targetedVRInteractable != null && heldVRInteractable == null)
                {
                    Interactions.Interact(targetedVRInteractable);
                    heldVRInteractable = targetedVRInteractable;
                }
            }
            if (Input.GetMouseButtonUp(0)) // left mouse up
            {
                if (heldVRInteractable != null)
                {
                    Interactions.AntiInteract(heldVRInteractable);
                    heldVRInteractable = null;
                }
            }

            // TODO: UI scrollbar dragging

            // scroll wheel
            if (Input.mouseScrollDelta.y != 0)
            {
                if (Input.GetMouseButton(1) || // zoom if not hovering over scrollable or if ctrl/RMB is being held
                    targetedVRInteractable == null ||
                    targetedVRInteractable.GetComponent<VRButton>() != null ||
                    targetedVRInteractable.GetComponent<VRInteractableUIButton>() != null ||
                    targetedVRInteractable.GetComponent<VRIHoverToggle>() != null)
                {
                    int newFOV = currentFOV + (Input.mouseScrollDelta.y < 0 ? 5 : -5);
                    SetCameraFOV(newFOV);
                }
                else if (targetedVRInteractable != null) // otherwise, scrollable interact
                {
                    // Scrollables interactables
                    VRTwistKnob twistKnob = targetedVRInteractable?.GetComponent<VRTwistKnob>();
                    VRTwistKnobInt twistKnobInt = targetedVRInteractable?.GetComponent<VRTwistKnobInt>();
                    VRLever lever = targetedVRInteractable?.GetComponent<VRLever>();
                    VRThrottle throttle = targetedVRInteractable?.GetComponent<VRThrottle>();
                    VRIntUIScroller uiScroll = targetedVRInteractable?.GetComponent<VRIntUIScroller>();

                    if (twistKnob != null)
                    {
                        Interactions.TwistKnob(twistKnob, Input.mouseScrollDelta.y < 0 ? true : false, 0.05f);
                    }
                    else if (twistKnobInt != null)
                    {
                        Interactions.MoveTwistKnobInt(twistKnobInt, Input.mouseScrollDelta.y < 0 ? 1 : -1, true);
                    }
                    else if (lever != null)
                    {
                        Interactions.MoveLever(lever, Input.mouseScrollDelta.y < 0 ? 1 : -1, true);
                    }
                    else if (throttle != null)
                    {
                        Interactions.MoveThrottle(throttle, Input.mouseScrollDelta.y > 0 ? -0.05f : 0.05f);
                    }
                    else if (uiScroll != null)
                    {
                        uiScroll.scrollRect.normalizedPosition += 0.1f * Input.mouseScrollDelta;
                    }
                }
            }
        }

        public void LateUpdate()
        {
            if (!flatScreenEnabled || cameraEyeGameObject == null)
                return;

            MoveCamera();
        }

        int frameTick = 0;
        const int FRAMES_PER_TICK = 60;
        const int FRAMES_PER_SUBTICK = 5;
        //bool wasOnTeam = false;
        public void FixedUpdate()
        {
            if (!flatScreenEnabled || cameraEyeGameObject == null)
                return;

            frameTick++;

            if (frameTick % FRAMES_PER_SUBTICK == 0) // every sub-tick
            {
                GetHoveredObject();
            }

            if (frameTick >= FRAMES_PER_TICK) // every tick
            {
                frameTick = 0;

                if (endMission == null && !endMissionWindowAutoShown)
                {
                    // check if mission has ended
                    EndMission endMission = GameObject.FindObjectOfType<EndMission>(false);
                    if (endMission != null && endMission.endScreenObject?.activeSelf == true)
                    {
                        FlatScreen2Plugin.Write("Ended mission!! Showing mission end window...");
                        this.endMission = endMission;
                        showEndMissionWindow = false;
                        ToggleEndMissionWindow();
                        endMissionWindowAutoShown = true;
                    }
                }

                SetTrackingObject(cameraEyeGameObject);

                VRInteractables = GameObject.FindObjectsOfType<VRInteractable>(false);

                //bool isOnTeam = VTOLMPSceneManager.instance?.localPlayer?.chosenTeam ?? false;
                //if (isOnTeam != wasOnTeam)
                //{
                //    RegrabTracks();
                //    wasOnTeam = isOnTeam;
                //}
            }
        }

        public void OnDestroy()
        {
            showWindow = false;
            showEndMissionWindow = false;

            if (flatScreenEnabled)
                VRHead.OnVRHeadChanged -= ResetState;

            instance = null;
        }
    }
}
