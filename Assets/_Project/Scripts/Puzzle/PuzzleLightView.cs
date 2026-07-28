using UnityEngine;

namespace Dangaronpo.Puzzle
{
    /// <summary>
    /// 单个机关灯的视觉表现。同步控制小圆片材质颜色/自发光和可选点光源。
    /// </summary>
    public class PuzzleLightView : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private int materialIndex;
        [SerializeField] private Light targetLight;
        [SerializeField] private Color offColor = new Color(0.08f, 0f, 0f);
        [SerializeField] private Color onColor = new Color(1f, 0.05f, 0.05f);
        [SerializeField] private Color emissionColor = new Color(1f, 0.05f, 0.05f);
        [SerializeField] private float emissionIntensity = 2.5f;
        [SerializeField] private bool litOnAwake;

        private Material runtimeMaterial;

        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();

            CacheRuntimeMaterial();
            SetLit(litOnAwake);
        }

        public void SetLit(bool lit)
        {
            if (runtimeMaterial != null)
            {
                // 修改 runtimeMaterial 不会直接污染项目里的共享材质资产。
                if (runtimeMaterial.HasProperty("_Color"))
                    runtimeMaterial.color = lit ? onColor : offColor;

                if (runtimeMaterial.HasProperty("_EmissionColor"))
                {
                    if (lit)
                    {
                        runtimeMaterial.EnableKeyword("_EMISSION");
                        runtimeMaterial.SetColor("_EmissionColor", emissionColor * emissionIntensity);
                    }
                    else
                    {
                        runtimeMaterial.SetColor("_EmissionColor", Color.black);
                    }
                }
            }

            if (targetLight != null)
                targetLight.enabled = lit;
        }

        private void CacheRuntimeMaterial()
        {
            if (targetRenderer == null)
                return;

            // Renderer.materials 会为运行时实例创建材质副本，适合每盏灯单独变色。
            Material[] materials = targetRenderer.materials;

            if (materials == null || materials.Length == 0)
                return;

            int safeIndex = Mathf.Clamp(materialIndex, 0, materials.Length - 1);
            runtimeMaterial = materials[safeIndex];
        }
    }
}
