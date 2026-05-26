using Game.Core.Events;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Win and lose screens share enough logic that they live in one file.
    /// Each is a thin overlay driven by a single event subscription.
    /// </summary>
    //public sealed class WinScreen : MonoBehaviour
    //{
    //    [SerializeField] private GameObject _root;
    //    private IEventBus _bus;

    //    public void Bind(IEventBus bus)
    //    {
    //        _bus = bus;
    //        _bus.Subscribe<GameWonEvent>(_ => { if (_root != null) _root.SetActive(true); });
    //        if (_root != null) _root.SetActive(false);
    //    }
    //}

    //public sealed class LoseScreen : MonoBehaviour
    //{
    //    [SerializeField] private GameObject _root;
    //    private IEventBus _bus;

    //    public void Bind(IEventBus bus)
    //    {
    //        _bus = bus;
    //        _bus.Subscribe<GameLostEvent>(_ => { if (_root != null) _root.SetActive(true); });
    //        if (_root != null) _root.SetActive(false);
    //    }
    //}
}
