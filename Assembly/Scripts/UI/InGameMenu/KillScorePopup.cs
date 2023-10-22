using UnityEngine.UI;
using UnityEngine;

namespace UI
{
    class KillScorePopup: BasePopup
    {
        protected override string Title => string.Empty;
        protected override float Width => 0f;
        protected override float Height => 0f;
        protected override float TopBarHeight => 0f;
        protected override float BottomBarHeight => 0f;
        protected override PopupAnimation PopupAnimationType => PopupAnimation.Tween;
        protected override float AnimationTime => 0.2f;
        private Text _scoreLabel;
        private Text _backgroundLabel;

        public override void Setup(BasePanel parent = null)
        {
            base.Setup(parent);
            _scoreLabel = ElementFactory.InstantiateAndBind(transform, "KillScoreLabel").GetComponent<Text>();
            _backgroundLabel = _scoreLabel.transform.Find("BackgroundLabel").GetComponent<Text>();
        }

        public void Show(int score)
        {
            _scoreLabel.text = score.ToString();
            _backgroundLabel.text = score.ToString();
            int fontSize = 40 + (int)(40f * Mathf.Min(score / 2000f, 1f));
            _scoreLabel.fontSize = fontSize;
            _backgroundLabel.fontSize = fontSize;
            IsActive = false;
            base.Show();
        }

        public void ShowSnapshotViewer(int score)
        {
            _scoreLabel.text = score.ToString();
            _backgroundLabel.text = score.ToString();
            int fontSize = 40;
            _scoreLabel.fontSize = fontSize;
            _backgroundLabel.fontSize = fontSize;
            base.ShowImmediate();
        }
    }
}
