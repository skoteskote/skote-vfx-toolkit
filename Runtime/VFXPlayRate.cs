using UnityEngine;
using UnityEngine.VFX;

namespace Skote.Vfx
{
    [RequireComponent(typeof(VisualEffect))]
    public class VFXPlayRate : MonoBehaviour
    {
        public int playRate = 50000000; 
        void Start()
        {
            GetComponent<VisualEffect>().SetInt("Rate", playRate);
        }
    }
}
