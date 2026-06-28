using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zillneuron.Belote.UI.Dialog.Parameter;
using Zillneuron.UILayout;

namespace Zillneuron.Belote.UI.Dialog
{    
    public class ResultDialog : ADialog
    {
        [SerializeField]
        private TMP_Text resultLabel;

        [SerializeField]
        private TMP_Text teamALabel;

        [SerializeField]
        private TMP_Text teamBLabel;

        [SerializeField]
        private TMP_Text orderLabel;

        [SerializeField]
        private TMP_Text orderA;

        [SerializeField]
        private Image orderATrump;

        [SerializeField]
        private TMP_Text orderB;

        [SerializeField]
        private Image orderBTrump;

        [SerializeField]
        private TMP_Text takenLabel;

        [SerializeField]
        private TMP_Text takenA;

        [SerializeField]
        private TMP_Text takenB;

        [SerializeField]
        private TMP_Text tierceLabel;

        [SerializeField]
        private TMP_Text tierceA;

        [SerializeField]
        private TMP_Text tierceB;

        [SerializeField]
        private TMP_Text blotLabel;

        [SerializeField]
        private TMP_Text blotA;

        [SerializeField]
        private TMP_Text blotB;

        [SerializeField]
        private TMP_Text fiftyLabel;

        [SerializeField]
        private TMP_Text fiftyA;

        [SerializeField]
        private TMP_Text fiftyB;

        [SerializeField]
        private TMP_Text hundredLabel;

        [SerializeField]
        private TMP_Text hundredA;

        [SerializeField]
        private TMP_Text hundredB;

        [SerializeField]
        private TMP_Text totalLabel;

        [SerializeField]
        private TMP_Text totalA;

        [SerializeField]
        private TMP_Text totalB;

        public override void SetUp(ADialogParameters parameters)
        {
            ResultDialogParameters resultDialogParameters = parameters as ResultDialogParameters;

            if (resultDialogParameters != null)
            {
                
            }
        }

        protected override void SetTexts()
        {
            base.SetTexts();

           
        }
    }
}