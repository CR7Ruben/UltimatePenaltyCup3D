using System;
using UnityEngine;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Customization;
using UltimatePenaltyCup.SaveSystem;

namespace UltimatePenaltyCup.Gameplay
{
    public class StadiumController : MonoBehaviour
    {
        public static StadiumController Instance { get; private set; }

        [Header("Stadium Renderers")]
        [SerializeField] private Renderer pitchRenderer;
        [SerializeField] private Renderer[] standRenderers;
        [SerializeField] private Renderer[] bannerRenderers;

        [Header("Grass Materials for Venues")]
        [SerializeField] private Material matGrassCantera;
        [SerializeField] private Material matGrassNight;
        [SerializeField] private Material matGrassPro;
        [SerializeField] private Material matGrassWorldCup;

        [Header("Lighting Components")]
        [SerializeField] private Light mainDirectionalLight;
        [SerializeField] private Light[] floodlights;
        [SerializeField] private ParticleSystem confettiSystem;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (CustomizationManager.Instance != null)
            {
                CustomizationManager.Instance.OnStadiumSelected += ApplyVenue;
                ApplyVenue(CustomizationManager.Instance.SelectedStadium);
            }
            else if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                ApplyVenue(SaveManager.Instance.CurrentData.selectedStadium);
            }
            else
            {
                ApplyVenue(StadiumType.Cantera);
            }
        }

        private void OnDestroy()
        {
            if (CustomizationManager.Instance != null)
            {
                CustomizationManager.Instance.OnStadiumSelected -= ApplyVenue;
            }
        }

        public void ApplyVenue(StadiumType venue)
        {
            Material targetGrass = venue switch
            {
                StadiumType.Cantera      => matGrassCantera,
                StadiumType.Night        => matGrassNight,
                StadiumType.Professional => matGrassPro,
                StadiumType.WorldCup     => matGrassWorldCup,
                _                        => matGrassCantera
            };

            if (pitchRenderer != null && targetGrass != null)
            {
                pitchRenderer.material = targetGrass;
            }

            ConfigureLighting(venue);
        }

        private void ConfigureLighting(StadiumType venue)
        {
            switch (venue)
            {
                case StadiumType.Cantera:
                    // Bright midday training session
                    RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.7f);
                    if (mainDirectionalLight != null)
                    {
                        mainDirectionalLight.enabled = true;
                        mainDirectionalLight.color = new Color(1.0f, 0.98f, 0.92f);
                        mainDirectionalLight.intensity = 1.3f;
                        mainDirectionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    }
                    SetFloodlightsActive(false, 0f);
                    if (confettiSystem != null) confettiSystem.Stop();
                    break;

                case StadiumType.Night:
                    // Dramatic night match with intense 4-tower floodlighting
                    RenderSettings.ambientLight = new Color(0.08f, 0.12f, 0.22f);
                    if (mainDirectionalLight != null)
                    {
                        mainDirectionalLight.enabled = true;
                        mainDirectionalLight.color = new Color(0.35f, 0.45f, 0.65f);
                        mainDirectionalLight.intensity = 0.35f;
                        mainDirectionalLight.transform.rotation = Quaternion.Euler(75f, 20f, 0f);
                    }
                    SetFloodlightsActive(true, 5.0f);
                    if (confettiSystem != null) confettiSystem.Stop();
                    break;

                case StadiumType.Professional:
                    // Sunset / twilight match with vibrant stadium atmosphere
                    RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.55f);
                    if (mainDirectionalLight != null)
                    {
                        mainDirectionalLight.enabled = true;
                        mainDirectionalLight.color = new Color(1.0f, 0.92f, 0.85f);
                        mainDirectionalLight.intensity = 1.1f;
                        mainDirectionalLight.transform.rotation = Quaternion.Euler(40f, -60f, 0f);
                    }
                    SetFloodlightsActive(true, 3.2f);
                    if (confettiSystem != null) confettiSystem.Stop();
                    break;

                case StadiumType.WorldCup:
                    // Championship final: brilliant gold illumination and festive confetti
                    RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.62f);
                    if (mainDirectionalLight != null)
                    {
                        mainDirectionalLight.enabled = true;
                        mainDirectionalLight.color = new Color(1.0f, 0.96f, 0.88f);
                        mainDirectionalLight.intensity = 1.4f;
                        mainDirectionalLight.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
                    }
                    SetFloodlightsActive(true, 4.5f);
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
            foreach (var light in floodlights)
            {
                if (light != null)
                {
                    light.enabled = active;
                    light.intensity = intensity;
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

            if (floodlights == null || floodlights.Length == 0)
            {
                floodlights = GetComponentsInChildren<Light>(true);
            }
        }
    }
}
