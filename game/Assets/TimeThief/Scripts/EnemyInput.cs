using UnityEngine;
using UnityEngine.EventSystems;

namespace TimeThief
{
    public sealed class EnemyInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IDragHandler
    {
        GameManager game;
        bool down, hold;
        int pointer;
        float duration;
        Vector2 position;
        void ReadPosition(PointerEventData e)
        {
            var rect = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var local))
                position = new Vector2((local.x - rect.rect.xMin) / rect.rect.width, (local.y - rect.rect.yMin) / rect.rect.height);
        }
        public void OnDrag(PointerEventData e) { if (down && e.pointerId == pointer) ReadPosition(e); }
        public bool Holding => hold && down;
        public void Init(GameManager g) => game = g;
        public void OnPointerDown(PointerEventData e)
        {
            if (down || !game.IsFighting || e.button != PointerEventData.InputButton.Left)
                return;
            down = true;
            hold = false;
            pointer = e.pointerId;
            duration = 0;
            ReadPosition(e);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!down || e.pointerId != pointer)
                return;
            bool tap = !hold && game.IsFighting;
            ReadPosition(e);
            Cancel();
            if (tap)
                game.enemy.Hit(false, 1, position.x, position.y);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (e.pointerId == pointer)
                Cancel();
        }

        public void Cancel()
        {
            down = hold = false;
            duration = 0;
            if (game && game.enemy != null)
                game.enemy.holdSeconds = 0;
            if (game && game.music)
                game.music.MagicLoop(false);
        }

        void Update()
        {
            if (!down)
                return;
            if (!game.IsFighting)
            {
                Cancel();
                return;
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f), old = duration;
            duration += dt;
            if (duration >= game.config.holdThreshold)
            {
                if (!hold)
                {
                    hold = true;
                    game.music.Sfx(game.config.magicStartSound);
                    game.music.MagicLoop(true);
                }

                float active = Mathf.Min(dt, duration - game.config.holdThreshold);
                game.enemy.holdSeconds += active;
                game.enemy.Hit(true, active, position.x, position.y);
            }
        }

        void OnDisable() => Cancel();
    }
}
