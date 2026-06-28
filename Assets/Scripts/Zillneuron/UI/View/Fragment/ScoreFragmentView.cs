using TMPro;
using UnityEngine;
using Zillneuron.UILayout;

namespace Zillneuron.Belote.UI.View.Fragment
{
    public class ScoreFragmentView : AFragmentView
    {
        [SerializeField]
        private TMP_Text firstTeamName;

        [SerializeField]
        private TMP_Text secondTeamName;

        [SerializeField]
        private TMP_Text firstTeamScore;

        [SerializeField]
        private TMP_Text secondTeamScore;

        [SerializeField]
        private TMP_Text targetScore;
    }
}