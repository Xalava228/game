using UnityEngine;
using UnityEngine.EventSystems;

namespace TimeThief
{
    public sealed class EnemyInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IInitializePotentialDragHandler
    {
        GameManager game;
        bool down, hold;
        int pointer;
        float duration;
        Vector2 position, screenPosition;
        Camera eventCamera;
        bool inside;
        void ReadPosition(PointerEventData e)
        {
            screenPosition = e.position;
            eventCamera = e.pressEventCamera;
            ReadPosition();
        }
        void ReadPosition()
        {
            var rect = (RectTransform)transform;
            inside = RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, eventCamera, out var local) && rect.rect.Contains(local);
            float side = Mathf.Max(1, Mathf.Min(rect.rect.width, rect.rect.height));
            position = new Vector2(.5f + local.x / side, .5f + local.y / side);
        }
        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;
        public void OnDrag(PointerEventData e) { if (down && e.pointerId == pointer) ReadPosition(e); }
        public bool Holding => hold && down && inside;
        public bool OnWeakPoint => inside && game && game.enemy != null && BossRules.InTarget(game.enemy, position.x, position.y);
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
            for (int i = 0; pointer >= 0 && i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.fingerId == pointer && touch.phase == TouchPhase.Canceled) { Cancel(); return; }
            }
            ReadPosition(e);
            bool tap = !hold && inside && game.IsFighting;
            Cancel();
            if (tap)
                game.enemy.Hit(false, 1, position.x, position.y);
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

            // Capture survives crossing the edge. Outside the stage damage pauses;
            // returning with the same finger resumes without an accidental tap.
            if (pointer < 0) screenPosition = Input.mousePosition;
            else
            {
                bool found = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.fingerId != pointer) continue;
                    if (touch.phase == TouchPhase.Canceled) { Cancel(); return; }
                    screenPosition = touch.position; found = true; break;
                }
                if (!found) { Cancel(); return; }
            }
            ReadPosition();
            if (!inside) { game.music.MagicLoop(false); return; }
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
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
                game.music.MagicLoop(true);
                game.enemy.holdSeconds += active;
                game.enemy.Hit(true, active, position.x, position.y);
            }
        }

        void OnDisable() => Cancel();
        void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
        void OnApplicationPause(bool paused) { if (paused) Cancel(); }
    }
}
