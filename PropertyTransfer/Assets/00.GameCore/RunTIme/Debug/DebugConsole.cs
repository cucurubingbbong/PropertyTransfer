using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    /// <summary>
    /// Ai 써서 만든 디버그 콘솔dd
    /// </summary>
    public class DebugConsole : Singleton<DebugConsole>
    {
        [Header("UI")]
        [SerializeField] private GameObject consoleRoot;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Input")]
        [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;

        [Header("Log")]
        [SerializeField] private int maxLogCount = 100;

        /// <summary>
        /// 디버그 명령어 딕셔너리
        /// </summary>
        private readonly Dictionary<string, DebugCommand> commands = new();

        /// <summary>
        /// 디버그 로그 리스트
        /// </summary>
        private readonly List<string> logs = new();

        private bool isOpen;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
                return;

            RegisterDefaultCommands();

            if (inputField != null)
                inputField.onSubmit.AddListener(OnSubmit);

            SetOpen(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                SetOpen(!isOpen);
            }
        }

        /// <summary>
        /// 디버그 콘솔 열기/닫기
        /// </summary>
        /// <param name="open"></param>
        private void SetOpen(bool open)
        {
            isOpen = open;

            if (consoleRoot != null)
                consoleRoot.SetActive(open);

            if (open)
                FocusInput();
        }

        /// <summary>
        /// 입력 필드를 선택합니다
        /// </summary>
        private void FocusInput()
        {
            if (inputField == null)
                return;

            inputField.ActivateInputField();
            inputField.Select();
        }

        /// <summary>
        /// 입력 필드에서 엔터를 눌렀을 때 호출되는 메서드
        /// </summary>
        /// <param name="input"></param>
        private void OnSubmit(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                FocusInput();
                return;
            }

            // 입력한 명령어를 로그에 출력하고 실행합니다
            Log($"> {input}");
            ExecuteInput(input);

            inputField.text = string.Empty;
            FocusInput();
        }

        /// <summary>
        /// 입력한 명령어를 실행합니다
        /// </summary>
        /// <param name="input"></param>
        private void ExecuteInput(string input)
        {
            // 공백 분리
            input = input.Trim();

            // 명령어 앞에 /가 붙어있으면 제거
            if (input.StartsWith("/")) input = input.Substring(1);

            // 명령어와 인자를 분리하니다
            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // 명령어가 없으면 종료
            if (parts.Length == 0)
                return;

            // 파트의 첫번째 요소를 명령어 이름으로 설정하고 소문자로 변환
            string commandName = parts[0].ToLower();

            string[] args = new string[parts.Length - 1];

            // 인자를 배열에 복사후 인자 배열을 명령어 실행에 전달
            for (int i = 1; i < parts.Length; i++)
            {
                args[i - 1] = parts[i];
            }

            // 명령어 딕셔너리에서 명령어 이름으로 명령어를 찾습니다
            if (!commands.TryGetValue(commandName, out DebugCommand command))
            {
                Log($"알 수 없는 명령어: {commandName}");
                return;
            }

            command.Execute(args);
        }

        /// <summary>
        /// 디버그 명령어를 등록합니다
        /// </summary>
        /// <param name="command"></param>
        public void RegisterCommand(DebugCommand command)
        {
            if (command == null)
                return;

            if (string.IsNullOrWhiteSpace(command.Name))
                return;

            commands[command.Name.ToLower()] = command;
        }

        /// <summary>
        /// 기본 디버그 명령어를 등록합니다
        /// </summary>
        private void RegisterDefaultCommands()
        {
            RegisterCommand(new DebugCommand(
                "help",
                "명령어 목록을 출력합니다.",
                "/help",
                args =>
                {
                    foreach (DebugCommand command in commands.Values)
                    {
                        Log($"{command.Usage} - {command.Description}");
                    }
                }
            ));

            RegisterCommand(new DebugCommand(
                "clear",
                "콘솔 로그를 지웁니다.",
                "/clear",
                args =>
                {
                    logs.Clear();
                    RefreshLogText();
                }
            ));

            RegisterCommand(new DebugCommand(
                "timescale",
                "게임 속도를 변경합니다.",
                "/timescale [value]",
                args =>
                {
                    if (args.Length <= 0)
                    {
                        Log("사용법: /timescale 1");
                        return;
                    }

                    if (!float.TryParse(args[0], out float value))
                    {
                        Log("숫자를 입력해야 합니다.");
                        return;
                    }

                    Time.timeScale = value;
                    Log($"TimeScale = {value}");
                }
            ));

            RegisterCommand(new DebugCommand(
                "scene",
                "씬을 이동합니다.",
                "/scene [sceneName]",
                args =>
                {
                    if (args.Length <= 0)
                    {
                        Log("사용법: /scene GameScene");
                        return;
                    }

                    SceneLoader.Instance.LoadScene(args[0]);
                    Log($"씬 이동 요청: {args[0]}");
                }
            ));

            RegisterCommand(new DebugCommand(
                "clearsave",
                "모든 저장 데이터를 삭제합니다.",
                "/clearsave",
                args =>
                {
                    SaveManager.Instance.DeleteAll();
                    Log("저장 데이터 삭제 완료");
                }
            ));
        }

        public void Log(string message)
        {
            logs.Add(message);

            if (logs.Count > maxLogCount)
                logs.RemoveAt(0);

            RefreshLogText();
        }

        private void RefreshLogText()
        {
            if (logText != null)
                logText.text = string.Join("\n", logs);

            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}