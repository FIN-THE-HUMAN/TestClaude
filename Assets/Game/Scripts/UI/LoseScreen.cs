using Game.Core.Events;
using UnityEngine;

namespace Game.UI
{
    public sealed class LoseScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        private IEventBus _bus;

        public void Bind(IEventBus bus)
        {
            _bus = bus;
            _bus.Subscribe<GameLostEvent>(_ => { if (_root != null) _root.SetActive(true); });
            if (_root != null) _root.SetActive(false);
        }
    }
}
