using UnityEngine;

namespace Mage
{
    /// <summary>소리 하나의 재생 데이터 (클립, 음량, 피치 범위). 콘텐츠 SO가 직접 가지고 AudioManager에 넘김</summary>
    [CreateAssetMenu(fileName = "SoundData", menuName = "Mage/Sound Data")]
    public sealed class SoundDataSO : ScriptableObject
    {
        /// <summary>낼 소리</summary>
        [SerializeField] AudioClip _clip;

        /// <summary>소스 음량 (0~1). 믹서 채널 음량과 곱해짐</summary>
        [SerializeField, Range(0f, 1f)] float _volume = 1f;

        /// <summary>재생마다 고를 피치 범위 (x 최소, y 최대). 둘 다 1이면 그대로. 피치 0이면 소리가 안 끝나서 하한 0.1</summary>
        [SerializeField, Min(0.1f)] Vector2 _pitchRange = Vector2.one;

        /// <summary>낼 소리</summary>
        public AudioClip Clip => _clip;

        /// <summary>소스 음량 (0~1)</summary>
        public float Volume => _volume;

        /// <summary>재생마다 고를 피치 범위 (x 최소, y 최대)</summary>
        public Vector2 PitchRange => _pitchRange;
    }
}
