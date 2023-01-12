using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Socket_Quartz_Manage.SocketBase
{
    public class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> m_pool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// <summary>
        /// 将实例添加到实例池
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item added to a SocketAsyncEventArgsPool cannot be null");
            }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        /// <summary>
        /// 从实例池中顶部取出一个实例
        /// </summary>
        /// <returns></returns>
        public SocketAsyncEventArgs Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        /// <summary>
        /// 获取实例池中实例数量
        /// </summary>
        public int Count
        {
            get { return m_pool.Count; }
        }

    }
}
