using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    class KillFeedPopup: BasePopup
    {
        protected override string Title => string.Empty;
        protected override float Width => 0f;
        protected override float Height => 0f;
        protected override float TopBarHeight => 0f;
        protected override float BottomBarHeight => 0f;
        protected override PopupAnimation PopupAnimationType => PopupAnimation.Fade;
        protected override float AnimationTime => 0.2f;
        private Text _leftLabel;
        private Text _rightLabel;
        private Text _scoreLabel;
        private Text _backgroundLabel;

        public override void Setup(BasePanel parent = null)
        {
            base.Setup(parent);
            var go = ElementFactory.InstantiateAndBind(transform, "KillFeedLabel");
            _leftLabel = go.transform.Find("LeftLabel").GetComponent<Text>();
            _rightLabel = go.transform.Find("RightLabel").GetComponent<Text>();
            _scoreLabel = go.transform.Find("ScoreLabel").GetComponent<Text>();
            _backgroundLabel = go.transform.Find("ScoreLabel/BackgroundLabel").GetComponent<Text>();
        }

        public void Show(string killer, string victim, int score)
        {
            _leftLabel.text = killer;
            _rightLabel.text = victim;
            _scoreLabel.text = score.ToString();
            _backgroundLabel.text = score.ToString();
            IsActive = false;
            base.Show();
        }
    }
}
