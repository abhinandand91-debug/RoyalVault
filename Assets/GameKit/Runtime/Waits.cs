using System.Collections.Generic;
using UnityEngine;

namespace GameKit
{
    // Wait objects made once per duration and shared: a coroutine that yields one allocates nothing after the first time
    public static class Waits
    {
        private static readonly Dictionary<float, WaitForSeconds> ByDuration = new Dictionary<float, WaitForSeconds>(32);

        public static WaitForSeconds Seconds(float seconds)
        {
            if (!ByDuration.TryGetValue(seconds, out WaitForSeconds wait))
            {
                wait = new WaitForSeconds(seconds);
                ByDuration[seconds] = wait;
            }

            return wait;
        }
    }
}
