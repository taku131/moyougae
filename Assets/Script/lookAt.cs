using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.Samples
{
    public class lookAt : MonoBehaviour
    {
        [SerializeField]
        private Transform _toRotate;

        [SerializeField]
        private Transform _target;

        void Start()
        {
            this.AssertField(_toRotate, nameof(_toRotate));
            this.AssertField(_target, "CenterEyeAnchor");
        }

        void Update()
        {
            Vector3 dirToTarget = (_target.position - _toRotate.position).normalized;
            _toRotate.LookAt(_toRotate.position - dirToTarget, Vector3.up);
        }
    }
}
