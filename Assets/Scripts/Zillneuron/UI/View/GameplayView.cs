using UnityEngine;
using UnityEngine.UI;
using Zillneuron.Belote.UI.View.Fragment;
using Zillneuron.UILayout;

namespace Zillneuron.Belote.UI.View
{
    public class GameplayView : ACompleteView
    {
        [SerializeField]
        private ScoreFragmentView scoreView;

        [SerializeField]
        private PreviousTrickFragmentView previousTrickView;

        [SerializeField]
        private TrickAreaFragmentView trickAreaView;

        [SerializeField]
        private PlayerFragmentView[] playerViews;

        [SerializeField]
        private PlayerHandFragmentView playerHandView;

        [SerializeField]
        private AIHandFragmentView[] aiHandViews;

        [SerializeField]
        private Button sureButton;
    }
}