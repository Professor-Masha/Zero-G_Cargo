using UnityEngine;
using UnityEngine.UI;
namespace ZeroGCargo.UI
{
    /// <summary>Non-numeric elapsed-time arc around the gameplay frame.</summary>
    public sealed class TimerArc : MaskableGraphic
    {
        private float fill;
        /// <summary>Consumed fraction, clamped to 0 through 1.</summary>
        public float Fill { get => fill; set { float next = Mathf.Clamp01(value); if (Mathf.Abs(next-fill)<.001f) return; fill = next; SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); if (fill <= 0) return;
            Rect rect = rectTransform.rect; const int segments = 180;
            float rx = rect.width*.5f-3, ry = rect.height*.5f-3;
            int steps = Mathf.CeilToInt(segments*fill);
            for (int i = 0; i < steps; i++)
            {
                float a = Mathf.PI*.5f - i/(float)segments*Mathf.PI*2;
                float b = Mathf.PI*.5f - Mathf.Min((i+1)/(float)segments,fill)*Mathf.PI*2;
                int start = helper.currentVertCount;
                helper.AddVert(new Vector3(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry,0),color,Vector2.zero);
                helper.AddVert(new Vector3(Mathf.Cos(a)*(rx-2),Mathf.Sin(a)*(ry-2),0),color,Vector2.zero);
                helper.AddVert(new Vector3(Mathf.Cos(b)*(rx-2),Mathf.Sin(b)*(ry-2),0),color,Vector2.zero);
                helper.AddVert(new Vector3(Mathf.Cos(b)*rx,Mathf.Sin(b)*ry,0),color,Vector2.zero);
                helper.AddTriangle(start,start+1,start+2); helper.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
