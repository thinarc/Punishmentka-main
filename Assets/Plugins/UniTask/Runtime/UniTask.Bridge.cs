#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections;

namespace Cysharp.Threading.Tasks
{
    // UnityEngine Bridges.

    public partial struct UniTask : IEquatable<UniTask>
    {
        public static IEnumerator ToCoroutine(Func<UniTask> taskFactory)
        {
            return taskFactory().ToCoroutine();
        }

        public bool Equals(UniTask other)
        {
            return Equals(source, other.source) && token == other.token;
        }

        public override bool Equals(object obj)
        {
            return obj is UniTask other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(source, token);
        }
    }
}

