using UnityEngine;
using Zillneuron.UILayout;

namespace Zillneuron.Belote.UI.View
{
    public class GameStartView : StartView
    {
        public void OnClick_Play()
        {
            ChangeActivityToRoot<GameplayView>();
        }
    }
}
