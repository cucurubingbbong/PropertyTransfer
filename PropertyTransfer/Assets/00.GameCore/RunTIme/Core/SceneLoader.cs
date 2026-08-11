using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore
{
    /// <summary>
    /// 씬 로딩을 담당하는 클래스
    /// </summary>
    public class SceneLoader : Singleton<SceneLoader>
    {
        public bool IsLoading { get; private set; }
        public float Progress { get; private set; }

        public void LoadScene(string sceneName)
        {
            if (IsLoading)
                return;

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsLoading = true;
            Progress = 0f;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            // 씬 로딩이 완료전까지 씬 전환 막기
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                Progress = Mathf.Clamp01(operation.progress / 0.9f);
                yield return null;
            }

            Progress = 1f;

            // 로딩 화면 넘어가기전까지 딜레이
            yield return new WaitForSeconds(0.3f);

            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                yield return null;
            }

            IsLoading = false;

            EventBus.Publish(new SceneLoadedEvent(sceneName));
        }
    }

    public readonly struct SceneLoadedEvent
    {
        public readonly string SceneName;

        public SceneLoadedEvent(string sceneName)
        {
            SceneName = sceneName;
        }
    }
}