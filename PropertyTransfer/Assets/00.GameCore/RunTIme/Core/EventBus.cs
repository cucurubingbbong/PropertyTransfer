using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 이벤트 버스 클래스
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type , Delegate> events = new();

        /// <summary>
        /// 이벤트를 구독하는 메서드
        /// </summary>
        /// <typeparam name="T">등록할 이벤트 타입</typeparam>
        /// <param name="listener">이벤트 리스너</param>
        public static void Subscribe<T>(Action<T> listener)
        {
            Type type = typeof(T);

            // 이벤트 타입이 이미 존재하면 기존 델리게이트에 리스너를 추가하고, 존재하지 않으면 새로 등록합니다.
            if(events.TryGetValue(type , out Delegate existing))
            {
                events[type] = Delegate.Combine(existing , listener);
            }
            else
            {
                events[type] = listener;
            }
        }

        /// <summary>
        /// 이벤트를 구독 해제하는 메서드
        /// </summary>
        /// <typeparam name="T">해제할 이벤트 타입</typeparam>
        /// <param name="listener">이벤트 리스너</param>
        public static void Unsubscribe<T>(Action<T> listener)
        {
            Type type = typeof(T);

            // 이벤트 타입이 존재하지 않으면 아무 작업도 수행하지 않습니다.

            if(!events.TryGetValue(type , out Delegate existing))
            {
                return;
            }

            // 기존 델리게이트에서 리스너를 제거하고, 제거 후 델리게이트가 null이면 이벤트 타입을 제거합니다.

            Delegate current = Delegate.Remove(existing , listener);

            if(current == null)
            {
                events.Remove(type);
            }
            else
            {
                events[type] = current;
            }
        }

        /// <summary>
        /// 이벤트를 발행하는 메서드
        /// </summary>
        /// <typeparam name="T">발행할 이벤트 타입</typeparam>
        /// <param name="eventData">이벤트 데이터</param>
        public static void Publish<T>(T eventData)
        {
            Type type = typeof(T);

            // 이벤트 타입이 존재한다면 해당 델리게이트를 호출하여 이벤트를 발행합니다.
            if(events.TryGetValue(type , out Delegate existing))
            {
                if(existing is Action<T> action)
                {
                    action.Invoke(eventData);
                }
            }
        }

        /// <summary>
        /// 모든 이벤트를 초기화하는 메서드
        /// </summary>
        public static void Clear()
        {
            events.Clear();
        }
    }
}
