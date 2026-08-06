using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 게임 저장 관리 클래스
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        public string SaveDirectory = null;

        protected override void Awake()
        {
            base.Awake();
            EnsureSaveDirectory();

        }

        /// <summary>
        /// 경로에 디렉토리가 있는지 검사하고 없다면 디렉토리를 생성합니다
        /// </summary>
        private void EnsureSaveDirectory()
        {
            SaveDirectory =  Application.persistentDataPath + "/Saves/";
            if (Directory.Exists(SaveDirectory) == false)
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }

        /// <summary>
        /// 데이터 저장 함수
        /// </summary>
        /// <typeparam name="T">저장할 데이터의 유형</typeparam>
        /// <param name="key">데이터를 식별할 키</param>
        /// <param name="data">저장할 데이터</param>
        public void Save<T>(string key, T data)
        {
            if (key == null) { Debug.Log("키가 비어있습니다"); return; }
            try
            {
                // JsonUtility를 이용해 Json화 , 줄바꿈과 들여쓰기를 위해 data뒤에 true 붙여줌
                string Json = JsonUtility.ToJson(data, true);
                string path = GetSavePath(key);

                File.WriteAllText(path, Json);

                Debug.Log($"저장 완료 {path}");
            }

            catch (Exception e)
            {
                Debug.Log($"저장 실패 {e}");
            }
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            if (key == null)
            {
                Debug.LogWarning("키가 비어있습니다");
                return defaultValue;
            }

            string path = GetSavePath(key);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"저장 파일 없음: {key}");
                return defaultValue;
            }

            try
            {
                string json = File.ReadAllText(path);
                T data = JsonUtility.FromJson<T>(json);

                Debug.Log($"불러오기 완료: {path}");

                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"불러오기 실패: {e.Message}");
                return defaultValue;
            }
        }

        public bool Exists(string key)
        {
            return File.Exists(GetSavePath(key));
        }

        public void Delete(string key)
        {
            string path = GetSavePath(key);

            if (!File.Exists(path))
                return;

            File.Delete(path);
            Debug.Log($"삭제 완료: {path}");
        }

        public void DeleteAll()
        {
            if (!Directory.Exists(SaveDirectory))
                return;

            Directory.Delete(SaveDirectory, true);
            Directory.CreateDirectory(SaveDirectory);

            Debug.Log("모든 저장 데이터 삭제 완료");
        }

        /// <summary>
        /// 데이터 경로 반환 함수
        /// </summary>
        /// <param name="key">데이터를 식별할 키</param>
        /// <returns></returns>

        private string GetSavePath(string key)
        {
            string safeKey = FileName(key);
            return Path.Combine(SaveDirectory, safeKey + ".json");
        }

        /// <summary>
        /// 파일 이름에 사용할수 없는 문자는 _로 변환해 반환
        /// </summary>
        /// <param name="fileName">원본 파일명</param>
        /// <returns></returns>

        private string FileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }

    }
}