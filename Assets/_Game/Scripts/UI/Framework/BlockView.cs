using System;
using System.Collections;
using UnityEngine;

namespace TCC.UI
{
    /// <summary>
    /// A full-screen black curtain. Its whole job is to help other interfaces fade:
    /// cover the screen, do something at the darkest point, then reveal. Sits at the
    /// top of the UI stack.
    /// </summary>
    public class BlockView : UIPanel<BlockView>
    {
        /// <summary>Fade to opaque black.</summary>
        public void Cover() => Show();

        /// <summary>Fade back to fully transparent.</summary>
        public void Uncover() => Hide();

        /// <summary>Curtain down, run <paramref name="midpoint"/> while black, curtain up.</summary>
        public void Transition(Action midpoint)
        {
            StartCoroutine(TransitionRoutine(midpoint));
        }

        private IEnumerator TransitionRoutine(Action midpoint)
        {
            Cover();
            yield return new WaitForSecondsRealtime(_fadeDuration + 0.02f);
            midpoint?.Invoke();
            yield return new WaitForSecondsRealtime(0.05f);
            Uncover();
        }
    }
}
