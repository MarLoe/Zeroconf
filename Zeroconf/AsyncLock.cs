﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Zeroconf
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    class AsyncLock
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        readonly SemaphoreSlim m_semaphore;
        readonly Task<Releaser> m_releaser;

        public AsyncLock()
        {
            m_semaphore = new SemaphoreSlim(1);
            m_releaser = Task.FromResult(new Releaser(this));
        }

        public struct Releaser : IDisposable
        {
            readonly AsyncLock m_toRelease;

            internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }

            public void Dispose()
            {
                if (m_toRelease != null)
                    m_toRelease.m_semaphore.Release();
            }
        }

#if DEBUG
        public Task<Releaser> LockAsync([CallerMemberName] string callingMethod = null, [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
        {
            Debug.WriteLine("AsyncLock.LockAsync called by: " + callingMethod + " in file: " + path + " : " + line);
#else
        public Task<Releaser> LockAsync()
        {
#endif
            var wait = m_semaphore.WaitAsync();

            return wait.IsCompleted ?
                m_releaser :
                wait.ContinueWith((_, state) => new Releaser((AsyncLock)state),
                    this, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

    }
}
