using System;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 게임 설정 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class GameSettingData
    {
        public float masterVolume = 1.0f;
        public float bgmVolume = 1.0f;
        public float sfxVolume = 1.0f;

        public bool isFullScreen = true;

        public Vector2Int resolution = new Vector2Int(1920, 1080);

        public string language = "en";
    }
}

