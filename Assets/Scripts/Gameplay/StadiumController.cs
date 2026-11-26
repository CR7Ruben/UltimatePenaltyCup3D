using UnityEngine;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Customization;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.SaveSystem;

namespace UltimatePenaltyCup.Gameplay
{
    public class StadiumController : MonoBehaviour
    {
        public static StadiumController Instance { get; private set; }

        [Header("Stadium Renderers")]
        [SerializeField] private Renderer pitchRenderer;
        [SerializeField] private Material bannerMaterial;

        [Header("Distinct Venue Hierarchies")]
        [SerializeField] private GameObject venueCantera;
        [SerializeField] private GameObject venueNight;
        [SerializeField] private GameObject venueProfessional;
        [SerializeField] private GameObject venueWorldCup;

        [Header("Grass Materials for Venues")]
        [SerializeField] private Material matGrassCantera;
        [SerializeField] private Material matGrassNight;
        [SerializeField] private Material matGrassPro;
        [SerializeField] private Material matGrassWorldCup;

        [Header("Lighting Components")]
        [SerializeField] private Light mainDirectionalLight;
        [SerializeField] private Light[] floodlights;
        [SerializeField] private ParticleSystem confettiSystem;

        private StadiumType activeVenue = StadiumType.Cantera;
        private Vector2 bannerScrollOffset = Vector2.zero;

        private void Awake()
        {
            Instance = this;
            AutoFindComponents();
        }

        private void Start()
        {
            if (CustomizationManager.Instance != null)
            {
                CustomizationManager.Instance.OnStadiumSelected += HandleStadiumSelected;
            }

            // If PenaltyManager didn't already apply round venue, apply default/saved
            if (PenaltyManager.Instance == null)
            {
                StadiumType initial = CustomizationManager.Instance != null
                    ? CustomizationManager.Instance.SelectedStadium
                    : StadiumType.Cantera;
                ApplyVenue(initial);
            }
        }

        private void OnDestroy()
        {
            if (CustomizationManager.Instance != null)
            {
                CustomizationManager.Instance.OnStadiumSelected -= HandleStadiumSelected;
            }
        }

        private void Update()
        {
            if (bannerMaterial != null && activeVenue != StadiumType.Cantera)
            {
                bannerScrollOffset.x = (bannerScrollOffset.x + Time.deltaTime * 0.06f) % 1f;
                bannerMaterial.mainTextureOffset = bannerScrollOffset;
                if (bannerMaterial.HasProperty("_BaseMap"))
                {
                    bannerMaterial.SetTextureOffset("_BaseMap", bannerScrollOffset);
                }
            }
        }

        private void HandleStadiumSelected(StadiumType stadium)
        {
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null && SaveManager.Instance.CurrentData.useCustomStadiumOverride)
            {
                ApplyVenue(stadium);
            }
            else if (GameManager.Instance != null && GameManager.Instance.CurrentRoundData != null)
            {
                ApplyVenue(GameManager.Instance.CurrentRoundData.stadiumTheme);
            }
        }

        public void ApplyForRound(RoundDataSO roundData)
        {
            var data = SaveManager.Instance != null ? SaveManager.Instance.CurrentData : null;
            if (data != null && data.useCustomStadiumOverride)
            {
                ApplyVenue(data.selectedStadium);
            }
            else if (roundData != null)
            {
                ApplyVenue(roundData.stadiumTheme);
            }
            else
            {
                ApplyVenue(StadiumType.Cantera);
            }
        }

        public void ApplyVenue(StadiumType venue)
        {
            activeVenue = venue;

            if (venueCantera != null) venueCantera.SetActive(venue == StadiumType.Cantera);
            if (venueNight != null) venueNight.SetActive(venue == StadiumType.Night);
            if (venueProfessional != null) venueProfessional.SetActive(venue == StadiumType.Professional);
            if (venueWorldCup != null) venueWorldCup.SetActive(venue == StadiumType.WorldCup);

            Material targetGrass = venue switch
            {
                StadiumType.Cantera => matGrassCantera,
                StadiumType.Night => matGrassNight,
                StadiumType.Professional => matGrassPro,
                StadiumType.WorldCup => matGrassWorldCup,
                _ => matGrassCantera
            };

            if (pitchRenderer != null && targetGrass != null)
            {
                pitchRenderer.sharedMaterial = targetGrass;
            }

            ConfigureLighting(venue);
        }

        private void ConfigureLighting(StadiumType venue)
        {
            if (mainDirectionalLight == null)
            {
                GameObject sunObj = GameObject.Find("Directional Light");
                if (sunObj != null) mainDirectionalLight = sunObj.GetComponent<Light>();
            }

            Camera cam = Camera.main;

            switch (venue)
            {
                case StadiumType.Cantera:
                    RenderSettings.ambientLight = new Color(0.62f, 0.68f, 0.75f);
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.64f, 0.80f, 0.96f);
                    RenderSettings.fogDensity = 0.0022f;
                    if (cam != null)
                    {
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = new Color(0.45f, 0.72f, 0.95f);
                    }
                    if (mainDirectionalLight != null)
                    {
                        mainDirectionalLight.enabled = true;
                        mainDirectionalLight.color = new Color(1.0f, 0.97f, 0.90f);
                        mainDirectionalLight.intensity = 1.35f;
                        mainDirectionalLight.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
                    }
                    SetFloodlightsActive(false, 0f);
                    if (confettiSystem != null) confettiSystem.Stop();
                    break;

                case StadiumType.Night:
                    RenderSettings.ambientLight = new Color(0.18f, 0.24f, 0.38f);
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.03f, 0.06f, 0.14f);
                    RenderSettings.fogDensity = 0.0038f;
                    if (cam != null)
                    {
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = new Color(0.03f, 0.06f, 0.14f);
                    }
                    if (mainDirectionalLight != null)
                    {
                        mainDirectionalLight.enabled = true;
                        mainDirectionalLight.color = new Color(0.65f, 0.78f, 1.0f);
                        mainDirectionalLight.intensity = 0.75f;
                        mainDirectionalLight.transform.rotation = Quaternion.Euler(65f, 25f, 0f);
                    }
                    SetFloodlightsActive(true, 45f);
                    if (confettiSystem != null) confettiSystem.Stop();
                    break;

                case StadiumType.Professional:
                    RenderSettings.ambientLight = new Color(0.48f, 0.50f, 0.58f);
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.24f, 0.34f, 0.54f);
                    RenderSettings.fogDensity = 0.0025f;
                    if (cam != null)
                    {
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = new Color(0.16f, 0.26f, 0.46f);
                    }
                    if (mainDirectionalLight != null)
                    {
                        mainDirectionalLight.enabled = true;
                        mainDirectionalLight.color = new Color(1.0f, 0.90f, 0.78f);
                        mainDirectionalLight.intensity = 1.2f;
                        mainDirectionalLight.transform.rotation = Quaternion.Euler(36f, -55f, 0f);
                    }
                    SetFloodlightsActive(true, 28f);
                    if (confettiSystem != null) confettiSystem.Stop();
                    break;

                case StadiumType.WorldCup:
                    RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.68f);
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.07f, 0.14f, 0.30f);
                    RenderSettings.fogDensity = 0.0022f;
                    if (cam != null)
                    {
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = new Color(0.06f, 0.12f, 0.28f);
                    }
                    if (mainDirectionalLight != null)
                    {
                        mainDirectionalLight.enabled = true;
                        mainDirectionalLight.color = new Color(1.0f, 0.96f, 0.88f);
                        mainDirectionalLight.intensity = 1.4f;
                        mainDirectionalLight.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
                    }
                    SetFloodlightsActive(true, 38f);
                    if (confettiSystem != null && !confettiSystem.isPlaying)
                    {
                        confettiSystem.Play();
                    }
                    break;
            }
        }

        private void SetFloodlightsActive(bool active, float intensity)
        {
            if (floodlights == null) return;
            foreach (var l in floodlights)
            {
                if (l != null)
                {
                    l.enabled = active;
                    l.intensity = intensity;
                }
            }
        }

        public void AutoFindComponents()
        {
            if (pitchRenderer == null)
            {
                Transform pitch = transform.Find("Pitch_Grass");
                if (pitch != null) pitchRenderer = pitch.GetComponent<Renderer>();
            }
            if (venueCantera == null) venueCantera = transform.Find("Venue_Cantera")?.gameObject;
            if (venueNight == null) venueNight = transform.Find("Venue_Night")?.gameObject;
            if (venueProfessional == null) venueProfessional = transform.Find("Venue_Professional")?.gameObject;
            if (venueWorldCup == null) venueWorldCup = transform.Find("Venue_WorldCup")?.gameObject;
        }
    }
}
