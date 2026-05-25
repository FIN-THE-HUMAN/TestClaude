using UnityEngine;

namespace Game.Monster
{
    /// <summary>
    /// Data definition for a monster type. Authored as a ScriptableObject so
    /// levels can swap visuals without changing scripts. Animation hooks are
    /// strings keyed into the visual's Animator parameter map; the system
    /// avoids strongly-typed AnimatorController dependencies here so the same
    /// definition can drive different rigs.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Monster/Monster Definition", fileName = "Monster_")]
    public sealed class MonsterDefinition : ScriptableObject
    {
        [SerializeField] private GameObject _viewPrefab;
        [SerializeField] private string _idleAnimTrigger = "Idle";
        [SerializeField] private string _eatAnimTrigger  = "Eat";
        [SerializeField] private string _runAnimBool     = "IsRunning";

        public GameObject ViewPrefab    => _viewPrefab;
        public string     IdleTrigger   => _idleAnimTrigger;
        public string     EatTrigger    => _eatAnimTrigger;
        public string     RunningBool   => _runAnimBool;
    }
}
