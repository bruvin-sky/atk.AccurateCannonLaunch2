using OWML.Common;
using OWML.ModHelper;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using AccurateCannonLaunch2;

namespace AccurateCannonLaunch
{
    public class AccurateCannonLaunch : ModBehaviour
    {
        public INewHorizons NewHorizons;
        private GameObject _giantsDeep;
        private GameObject _probeCannon;
        private GameObject _newProbeTrackingModule;
        private NomaiRemoteCameraPlatform _newProbeModulePool;
        private GameObject _origProbeModule;
        private NomaiRemoteCameraPlatform _origProbeModulePool;
        private OrbitalProbeLaunchController _launchController;
        private NomaiRemoteCameraPlatform _debugPool;
        //private OWRenderer _eyesRenderer;
        //private GameObject _fakeDebris1;
        //private GameObject _fakeDebris2;

        private UnityEngine.GameObject _cloudSplashEffect;
        private UnityEngine.GameObject _oceanSplashEffect;

        private bool _wokenUp;
        private bool _lightningFlashed;
        private bool _cloudSplashed;
        private bool _oceanSplashed;
        private bool _glowed;
        private bool _moduleSwapped;
        //private bool _debrisSwapped;

        private float _gdDistance;
        public float t = 0.0f;
        public static bool LaunchTowerPTMPool { get; set; }
        public static bool GabbroIsleSpawn { get; set; }

        // private bool _origProbeModuleDestroyed; 

        private void Awake()
        {
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }

        private void Start()
        {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"My mod {nameof(AccurateCannonLaunch)} is loaded! bleh", MessageType.Success);
            var newHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            newHorizons.LoadConfigs(this);

            // Example of accessing game code.
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);

                _giantsDeep = GameObject.Find("GiantsDeep_Body");
                _probeCannon = GameObject.Find("OrbitalProbeCannon_Body");

                _launchController = _probeCannon.GetComponent<OrbitalProbeLaunchController>();
                /*_fakeDebris1 = _launchController._fakeDebrisBodies[0].gameObject;
                _fakeDebris2 = _launchController._fakeDebrisBodies[1].gameObject;
                ModHelper.Console.WriteLine("found fakeDebris1 and fakeDebris2", MessageType.Success);*/
            };

            GlobalMessenger<int>.AddListener("StartOfTimeLoop", OnStartOfTimeLoop);
            GlobalMessenger.AddListener("WakeUp", OnWakeUp);
        }
        public override void Configure(IModConfig config)
        {
            LaunchTowerPTMPool = config.GetSettingsValue<bool>(nameof(LaunchTowerPTMPool));

            if (LoadManager.s_currentScene == OWScene.SolarSystem)
            {
                if (LaunchTowerPTMPool)
                {
                    DialogueConditionManager.s_instance.SetConditionState("FAITH_PTM_POOL_DEBUG", true);
                }
                else
                {
                    DialogueConditionManager.s_instance.SetConditionState("FAITH_PTM_POOL_DEBUG", false);
                    var itemTool = Locator.GetToolModeSwapper().GetItemCarryTool();
                    if (itemTool._heldItem != null && itemTool._heldItem.name == "startcamp_SharedStone")
                    {
                        GameObject.Destroy(itemTool._heldItem.gameObject);
                    }
                }
            }

            GabbroIsleSpawn = config.GetSettingsValue<bool>(nameof(GabbroIsleSpawn));
            /*if (PlayerData._currentGameSave != null)
            {
                if (GabbroIsleSpawn)
                {
                    PlayerData._currentGameSave.SetPersistentCondition("GabbroIsleSpawnPersist", true);
                }
                else
                {
                    PlayerData._currentGameSave.SetPersistentCondition("GabbroIsleSpawnPersist", false);
                }
            }*/
        }

        private void OnDestroy()
        {
            GlobalMessenger<int>.RemoveListener("StartOfTimeLoop", OnStartOfTimeLoop);
            GlobalMessenger.RemoveListener("WakeUp", OnWakeUp);
        }

        private void OnStartOfTimeLoop(int loopCount)
        {
            _wokenUp = false;
            _lightningFlashed = false;
            _oceanSplashed = false;
            _cloudSplashed = false;
            _glowed = false;
            _moduleSwapped = false;
            t = 0;
            // _debrisSwapped = false; 


            if (_launchController.enabled)
            {
                _newProbeTrackingModule = _probeCannon.transform.Find("Sector_OrbitalProbeCannon/ProbeTracker Module").gameObject;
                //_eyesRenderer = _newProbeTrackingModule.transform.Find("Props_Module_Sunken/Prefab_NOM_StatueHead/Statue_Eyes").gameObject.AddComponent<OWRenderer>();
                //_eyesRenderer.SetEmissionColor(new Color(0.5294f, 0.5763f, 1.5f, 1));
                var gdInteriorSector = _giantsDeep.transform.Find("Sector_GD/Sector_GDInterior/Sector_GDSurface").GetComponent<Sector>();
                _newProbeModulePool = _newProbeTrackingModule.transform.Find("Interactables_Module_Sunken/Prefab_NOM_RemoteViewer (1)").GetComponent<NomaiRemoteCameraPlatform>();
                _newProbeModulePool._visualSector2 = gdInteriorSector;
                _newProbeTrackingModule.transform.Find("Effects_Module_Sunken/sunkenModuleStencil").transform.localScale = new Vector3(17.5f, 17.5f, 17.5f);

                _origProbeModule = _giantsDeep.transform.Find("Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken").gameObject;
                if (_origProbeModule != null)
                {
                    _origProbeModulePool = _origProbeModule.transform.Find("Interactables_Module_Sunken/Prefab_NOM_RemoteViewer (1)").GetComponent<NomaiRemoteCameraPlatform>();
                    _origProbeModulePool._id = NomaiRemoteCameraPlatform.ID.None;
                    _origProbeModulePool.gameObject.SetActive(false);
                    _newProbeModulePool._socket._sector = _origProbeModulePool._socket._sector;
                    _origProbeModule.gameObject.SetActive(false);
                }

                /*var animator = _newProbeTrackingModule.AddComponent<TransformAnimator>();
                animator._origLocalPosition = _newProbeTrackingModule.transform.localPosition;
                animator._origLocalRotation = _newProbeTrackingModule.transform.localRotation;
                animator.TranslateToLocalPosition(new Vector3(-32.4f, -75.2f, -32f), 25);
                animator.RotateToLocalEulerAngles(new Vector3(8.224f, 139.4539f, 93.1895f), 25);*/

                _cloudSplashEffect = Locator.GetPlayerDetector().GetComponent<DynamicFluidDetector>()._splashEffects[1].splashPrefab.gameObject;
                _oceanSplashEffect = Locator.GetPlayerDetector().GetComponent<DynamicFluidDetector>()._splashEffects[0].splashPrefab.gameObject;

                _wokenUp = true;
            }

            {
                if (GabbroIsleSpawn)
                {
                    var _gabbroSpawn = GameObject.Find("GabbroIsland_Body/PlayerSpawnPoint").GetComponent<SpawnPoint>();
                    Locator._playerBody.GetComponent<PlayerSpawner>()._initialSpawnPoint = _gabbroSpawn;
                    Locator._timberHearth.gameObject.transform.Find("Sector_TH/Sector_Village/Volumes_Village/MusicVolume_Village").gameObject.SetActive(false);
                }
                else
                {
                    var goddamnDumbSpawn = Locator._timberHearth.transform.Find("Sector_TH/Sector_Village/Interactables_Village/LaunchTower/Spawn_TH").GetComponent<SpawnPoint>();
                    Locator._playerBody.GetComponent<PlayerSpawner>()._initialSpawnPoint = goddamnDumbSpawn;
                }
            }
        }

        private void OnWakeUp()
        {
            _debugPool = Locator._timberHearth.transform.Find("Sector_TH/startcamp_RemoteViewer").GetComponent<NomaiRemoteCameraPlatform>();
            _debugPool._id = NomaiRemoteCameraPlatform.ID.None;
            if (LaunchTowerPTMPool)
            {
                DialogueConditionManager.s_instance.SetConditionState("FAITH_PTM_POOL_DEBUG", true);
                // StartCoroutine("IntroPoolStart");
                var givemetheEhstone = _debugPool.transform.Find("startcamp_SharedStone").GetComponent<SharedStone>();
                _debugPool._socket.PlaceIntoSocket(givemetheEhstone);
                /*var itemTool = Locator.GetToolModeSwapper().GetItemCarryTool();
                itemTool.MoveItemToCarrySocket(givemetheEhstone.GetComponent<OWItem>());
                itemTool._heldItem = givemetheEhstone.GetComponent<OWItem>();*/
            }
            else
            {
                DialogueConditionManager.s_instance.SetConditionState("FAITH_PTM_POOL_DEBUG", false);
            }
        }
        /*private void IntroPoolStart()
        {
            var givemetheEhstone = _debugPool.transform.Find("startcamp_SharedStone");
            if (givemetheEhstone != null)
            {
                new WaitForSeconds(1);
                _debugPool._socket.PlaceIntoSocket(givemetheEhstone.GetComponent<SharedStone>());
            }
        }*/

        private void Update()
        {
            if (_newProbeTrackingModule != null && _origProbeModule != null && _wokenUp)
            {
                /*if (_launchController._hasLaunchedProbe && Time.time >= _launchController._probeLaunchTime + 3 && !_debrisSwapped)
                {
                    var animator1 = _fakeDebris1.AddComponent<TransformAnimator>();
                    //animator1._origLocalPosition = _fakeDebris1.transform.localPosition;
                    animator1.TranslateToLocalPosition(_launchController._realDebrisSectorProxies[0].transform.root.localPosition, 20);
                    animator1.RotateToLocalEulerAngles(_launchController._realDebrisSectorProxies[0].transform.root.localEulerAngles, 20);
                    var animator2 = _fakeDebris2.AddComponent<TransformAnimator>();
                    //animator2._origLocalPosition = _fakeDebris2.transform.localPosition;
                    animator2.TranslateToLocalPosition(_launchController._realDebrisSectorProxies[1].transform.root.localPosition, 20);
                    animator2.RotateToLocalEulerAngles(_launchController._realDebrisSectorProxies[1].transform.root.localEulerAngles, 20);
                    _debrisSwapped = true;
                }*/

                if (_launchController._hasLaunchedProbe && !_moduleSwapped)
                {
                    var baseCloudLight = _giantsDeep.transform.Find("Sector_GD/Clouds_GD/LightningGenerator_GD").gameObject.GetComponent<CloudLightningGenerator>();
                    var baseRedLight = baseCloudLight._lightColor;

                    if (Time.time >= _launchController._probeLaunchTime + 1.5 && !_moduleSwapped)
                    {
                        if (!_lightningFlashed)
                        {
                            if (GabbroIsleSpawn)
                            {
                                Locator._playerBody.GetComponent<PlayerSpacesuit>().SuitUp(instantSuitUp: true);
                            }
                            baseCloudLight._audioSourcePool.Peek().AssignAudioLibraryClip(AudioType.EyeBigBang);
                            baseCloudLight.SpawnLightning(_probeCannon.transform.position);
                            foreach (var lightning in baseCloudLight.gameObject.transform.GetComponentsInChildren<CloudLightning>())
                            {
                                lightning.lightColor = new Color(0.44f, 0.2f, 1, 1);
                            }
                            var gdCore = _giantsDeep.transform.Find("Sector_GD/Sector_GDInterior/Sector_GDCore");
                            _newProbeTrackingModule.transform.SetParent(gdCore.transform);

                            _lightningFlashed = true;
                        }
                        /*if (!_origProbeModuleDestroyed)
                        {
                            _origProbeModule = _giantsDeep.transform.Find("Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken").gameObject;
                            if (_origProbeModule != null)
                            {
                                _origProbeModule.gameObject.SetActive(false);
                                _origProbeModulePool = _origProbeModule.transform.Find("Interactables_Module_Sunken/Prefab_NOM_RemoteViewer (1)").GetComponent<NomaiRemoteCameraPlatform>();
                                _origProbeModulePool._id = NomaiRemoteCameraPlatform.ID.None;
                                _origProbeModuleDestroyed = true;
                            }
                        }*/

                        if (!OWTime.IsPaused() && !_moduleSwapped)
                        {
                            t += UnityEngine.Time.deltaTime;

                            var startingPos = _newProbeTrackingModule.transform.localPosition;
                            var endPos = new Vector3(-32.4f, -75.2f, -32f);
                            _newProbeTrackingModule.transform.localPosition = Vector3.Lerp(startingPos, endPos, (t / 2050f));
                            var startingRot = _newProbeTrackingModule.transform.localEulerAngles;
                            var endRot = new Vector3(8.224f, 139.4539f, 93.1895f);
                            _newProbeTrackingModule.transform.localEulerAngles = Vector3.Lerp(startingRot, endRot, (t / 2050f));
                        }
                    }

                    _gdDistance = Vector3.Distance(_giantsDeep.transform.position, _newProbeTrackingModule.transform.position);
                    if (_gdDistance < 965f && !_cloudSplashed)
                    {
                        //_eyesRenderer.SetEmissionColor(Color.Lerp(Color.black, new Color(0.5294f, 0.5763f, 1.5f, 1), Mathf.MoveTowards(1, 0f, Time.deltaTime)));
                        var cloudSplash = GameObject.Instantiate(_cloudSplashEffect, _newProbeTrackingModule.transform);
                        cloudSplash.transform.position = _newProbeTrackingModule.transform.position;
                        cloudSplash.AddComponent<AlignWithTargetBody>()._targetBody = _giantsDeep.GetComponent<OWRigidbody>();
                        //cloudSplash.transform.rotation = _newProbeTrackingModule.transform.rotation;
                        cloudSplash.transform.localScale = new Vector3(10, 10, 10);

                        foreach (var lightning in baseCloudLight.gameObject.transform.GetComponentsInChildren<CloudLightning>())
                        {
                            lightning.lightColor = new Color(0.9133f, 0.3814f, 0.6137f, 1);
                        }
                        foreach (var light in baseCloudLight.gameObject.transform.GetComponentsInChildren<OWAudioSource>())
                        {
                            light.AssignAudioLibraryClip(AudioType.GD_Lightning);
                        }
                        _newProbeTrackingModule.transform.Find("Lighting_Module_Sunken/FillLight_SunkenModule").GetComponent<OWLight>().FadeTo(0, 4);
                        _cloudSplashed = true;
                    }
                    if (_gdDistance < 500f && !_oceanSplashed)
                    {
                        var waveSplash = _giantsDeep.transform.Find("Sector_GD/Sector_GDInterior/Ocean_GD").GetComponent<OceanEffectController>();
                        waveSplash.CreateSplash(_newProbeTrackingModule.transform.position, 200, 5, 30, 30);
                        var oceanSplash = GameObject.Instantiate(_oceanSplashEffect, _newProbeTrackingModule.transform);
                        oceanSplash.GetComponent<SelfDestruct>()._secondsUntilSelfDestruct = 6;
                        oceanSplash.transform.position = _newProbeTrackingModule.transform.position;
                        oceanSplash.AddComponent<AlignWithTargetBody>()._targetBody = _giantsDeep.GetComponent<OWRigidbody>();
                        //oceanSplash.transform.rotation = _newProbeTrackingModule.transform.rotation;
                        oceanSplash.transform.localScale = new Vector3(10, 10, 10);
                        oceanSplash.GetComponent<SplashAudioController>()._splashClip = AudioType.GD_IslandSplash;
                        oceanSplash.GetComponent<SplashAudioController>().PlaySplash();
                        _oceanSplashed = true;
                    }

                    if (_gdDistance < 90f && !_glowed)
                    {
                        _newProbeTrackingModule.transform.Find("Lighting_Module_Sunken/FillLight_SunkenModule").GetComponent<OWLight>().FadeTo(1, 3);
                        _glowed = true;
                    }


                    if (TimeLoop.GetSecondsElapsed() > 30 && !_moduleSwapped)
                    {
                        _newProbeModulePool.transform.SetParent(_origProbeModule.transform);
                        _newProbeModulePool.transform.localPosition = new Vector3(0, 0, 8.7501f);
                        _newProbeModulePool.transform.localEulerAngles = new Vector3(90, 180, 0);
                        if (_origProbeModule != null)
                        {
                            _origProbeModule.SetActive(true);
                            var airlock = _origProbeModule.gameObject.transform.Find("Interactables_GDCore/Prefab_NOM_Airlock/AirlockController").GetComponent<NomaiAirlock>();
                            airlock._listInterfaceOrb[0].enabled = true;
                        }
                        _newProbeTrackingModule.SetActive(false);
                        _newProbeModulePool._visualSector = _origProbeModulePool._visualSector;
                        _moduleSwapped = true;
                    }
                }


            }
        }

    }

}
