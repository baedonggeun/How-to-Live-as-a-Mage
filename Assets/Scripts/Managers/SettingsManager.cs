using System;
using System.IO;
using UnityEngine;

namespace Mage
{
    /// <summary>설정 파일 담당. 지금은 기기 설정(settings_device.json) 하나. 세이브 슬롯과 따로 persistentDataPath에 둠</summary>
    public sealed class SettingsManager : MonoSingleton<SettingsManager>
    {
        /// <summary>기기 설정 파일 이름 (persistentDataPath 바로 아래)</summary>
        const string DeviceFileName = "settings_device.json";

        /// <summary>임시 파일 (본 파일 이름 + 이것). 다 쓴 뒤 본 파일과 바꿈</summary>
        const string TempSuffix = ".tmp";

        /// <summary>바로 전 본 파일 (본 파일 이름 + 이것). 본 파일이 없거나 깨졌을 때 읽음</summary>
        const string BackupSuffix = ".bak";

        /// <summary>깨진 본 파일을 옮겨 둘 이름 (본 파일 이름 + 이것). 다음 저장이 깨진 내용으로 .bak을 덮지 않게</summary>
        const string CorruptSuffix = ".corrupt";

        /// <summary>기기 설정 파일 전체 경로</summary>
        string _devicePath;

        /// <summary>지금 기기 설정. InitSingleton 뒤엔 늘 있음</summary>
        DeviceSettingsEntry _device;

        /// <summary>지금 기기 설정 (음량). 바꾼 값은 Save를 불러야 파일에 남음</summary>
        public DeviceSettingsEntry Device => _device;

        #region InitSingleton - 기기 설정 읽기: 본 파일 → .bak → 기본값 (다른 매니저는 Start부터 Device를 씀)
        protected override void InitSingleton()
        {
            _devicePath = Path.Combine(Application.persistentDataPath, DeviceFileName);
            _device = ReadDevice(_devicePath, moveIfCorrupt: true);
            if (_device == null)
            {
                _device = ReadDevice(_devicePath + BackupSuffix, moveIfCorrupt: false);
                if (_device != null)
                    Debug.LogWarning($"[Settings] 본 파일 대신 백업에서 읽음: {_devicePath}{BackupSuffix}");
            }
            if (_device == null) _device = new DeviceSettingsEntry();
        }
        #endregion

        #region Save - 기기 설정을 파일에 씀. .tmp에 다 쓴 뒤 본 파일과 바꾸고 전 본 파일은 .bak (쓰다 꺼져도 본 파일이 안 깨지게). 실패하면 에러만 남기고 지금 값은 그대로 씀
        public void Save()
        {
            if (_device == null) return;
            // 다 쓴 뒤 본 파일과 바꿀 임시 파일
            string tmp = _devicePath + TempSuffix;
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(tmp, JsonUtility.ToJson(_device, true));
                // File.Replace는 바꿀 본 파일이 있어야 함. 처음 저장·깨진 파일을 옮긴 뒤엔 이름만 바꿈
                if (File.Exists(_devicePath))
                    File.Replace(tmp, _devicePath, _devicePath + BackupSuffix);
                else
                    File.Move(tmp, _devicePath);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Debug.LogError($"[Settings] 저장 실패: {_devicePath} ({e.Message}). 이번 실행 동안은 바뀐 값 그대로 씀");
            }
        }
        #endregion

        #region OnApplicationQuit - 종료 표시 후 저장 (설정 창을 연 채 끄는 경우. 에디터는 Play 정지)
        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            Save();
        }
        #endregion

        #region ReadDevice - 파일 하나에서 기기 설정 읽기. 없거나 못 읽거나 깨졌으면 null
        /// <param name="path">읽을 파일</param>
        /// <param name="moveIfCorrupt">깨졌으면 .corrupt로 옮길지 (본 파일만 true)</param>
        static DeviceSettingsEntry ReadDevice(string path, bool moveIfCorrupt)
        {
            if (!File.Exists(path)) return null;
            // 파일 내용
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Debug.LogError($"[Settings] 못 읽음: {path} ({e.Message})");
                return null;
            }

            // 읽은 값. 파일에 없는 키는 기본값 그대로
            var entry = new DeviceSettingsEntry();
            // 깨진 이유. null이면 정상
            string error = null;
            if (string.IsNullOrWhiteSpace(json))
                error = "빈 파일";
            else
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(json, entry);
                }
                catch (ArgumentException e)
                {
                    error = e.Message;
                }
            }
            if (error != null)
            {
                Debug.LogWarning($"[Settings] 깨진 설정 파일: {path} ({error})");
                if (moveIfCorrupt) MoveAside(path);
                return null;
            }

            if (entry.Validate())
                Debug.LogWarning($"[Settings] 잘못된 값을 고쳐 읽음: {path}");
            return entry;
        }
        #endregion

        #region MoveAside - 깨진 파일을 .corrupt로 옮김 (전에 옮겨 둔 게 있으면 지우고)
        /// <param name="path">옮길 파일</param>
        static void MoveAside(string path)
        {
            // 옮겨 둘 이름
            string corrupt = path + CorruptSuffix;
            try
            {
                File.Delete(corrupt);
                File.Move(path, corrupt);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Debug.LogError($"[Settings] 깨진 파일 못 옮김: {path} ({e.Message})");
            }
        }
        #endregion
    }
}
