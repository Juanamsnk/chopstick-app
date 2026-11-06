using System;
using System.Collections.Generic;
using Chopsticks.Social;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Clasificacion general: los jugadores con mas victorias en partidas online (el modo carrera no cuenta).
    /// De arriba abajo: tu posicion, el podio con los 3 primeros y la lista del resto.
    /// La estructura esta en Leaderboard.uxml y el aspecto en Social.uss; aqui solo se rellenan datos.
    /// Los datos se los da SocialController.
    /// </summary>
    public class LeaderboardView
    {
        private readonly Label messageLabel;
        private readonly Button refreshButton;
        private readonly VisualElement meCard;
        private readonly VisualElement meBadge;
        private readonly VisualElement meInfo;
        private readonly Label mePlace;
        private readonly Label meTitle;
        private readonly VisualElement meChips;
        private readonly Label meEmpty;
        private readonly VisualElement podiumBox;
        private readonly VisualElement rowsBox;

        public event Action RefreshClicked;

        public LeaderboardView(VisualElement screen)
        {
            messageLabel = screen.Q<Label>("lb-msg");
            meCard = screen.Q("lb-me");
            meBadge = screen.Q("lb-me-badge");
            meInfo = screen.Q("lb-me-info");
            mePlace = screen.Q<Label>("lb-me-place");
            meTitle = screen.Q<Label>("lb-me-title");
            meChips = screen.Q("lb-me-chips");
            meEmpty = screen.Q<Label>("lb-me-empty");
            podiumBox = screen.Q("podium");
            rowsBox = screen.Q("lb-rows");
            refreshButton = screen.Q("action-row").Q<Button>("btn-primary");
            refreshButton.clicked += () => { if (RefreshClicked != null) RefreshClicked(); };
        }

        public void ShowLoading()
        {
            Clear();
            SetMessage("Cargando...", false);
            refreshButton.SetEnabled(false);
        }

        public void ShowError(string text)
        {
            Clear();
            SetMessage(text, true);
            refreshButton.SetEnabled(true);
        }

        /// <summary>Pinta la lista (ya ordenada de mas a menos victorias) y marca tu fila.</summary>
        public void ShowEntries(List<LeaderboardEntry> entries, string myUid, RecordInfo mine)
        {
            Clear();
            refreshButton.SetEnabled(true);

            int myPosition = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Uid == myUid) { myPosition = i + 1; break; }
            }

            FillMyCard(mine, myPosition, entries.Count);
            SetMessage(entries.Count == 0 ? "Todavía nadie ha ganado una partida online. ¡Sé el primero!" : "", false);

            // Podio con los tres primeros; si hay menos de tres, todos van en la lista
            int listStart = 0;
            if (entries.Count >= 3)
            {
                podiumBox.style.display = DisplayStyle.Flex;
                int[] order = { 1, 0, 2 };   // segundo, primero, tercero: el primero queda en el centro
                foreach (int index in order) podiumBox.Add(BuildPodiumColumn(index + 1, entries[index], entries[index].Uid == myUid));
                listStart = 3;
            }

            for (int i = listStart; i < entries.Count; i++)
            {
                rowsBox.Add(BuildRow(i + 1, entries[i], entries[i].Uid == myUid));
            }
        }

        // ------------------------------------------------------------
        // Piezas
        // ------------------------------------------------------------
        private void Clear()
        {
            meCard.style.display = DisplayStyle.None;
            podiumBox.Clear();
            podiumBox.style.display = DisplayStyle.None;
            rowsBox.Clear();
        }

        /// <summary>Tarjeta con tu puesto y tu marcador.</summary>
        private void FillMyCard(RecordInfo mine, int position, int total)
        {
            meCard.style.display = DisplayStyle.Flex;
            bool played = mine != null && mine.Played > 0;

            meBadge.style.display = played ? DisplayStyle.Flex : DisplayStyle.None;
            meInfo.style.display = played ? DisplayStyle.Flex : DisplayStyle.None;
            meEmpty.style.display = played ? DisplayStyle.None : DisplayStyle.Flex;

            if (!played)
            {
                meEmpty.text = "Todavía no has jugado partidas online. ¡Juega una para entrar en la clasificación!";
                return;
            }

            mePlace.text = position > 0 ? "#" + position : "—";
            meTitle.text = position > 0 ? "Tu posición" : "Fuera del top " + total;

            meChips.Clear();
            meChips.Add(SocialStyle.Chip(SocialStyle.Plural(mine.Wins, "victoria", "victorias"), "chip--win"));
            meChips.Add(SocialStyle.Chip(SocialStyle.Plural(mine.Losses, "derrota", "derrotas"), "chip--lose"));
            meChips.Add(SocialStyle.Chip(SocialStyle.WinRate(mine.Wins, mine.Losses) + "%"));
        }

        /// <summary>Una columna del podio: avatar con aro de medalla, nombre, victorias y peana con el numero.</summary>
        private static VisualElement BuildPodiumColumn(int place, LeaderboardEntry entry, bool isMe)
        {
            var column = new VisualElement();
            column.AddToClassList("podium__col");

            string medal = place == 1 ? "avatar--gold" : place == 2 ? "avatar--silver" : "avatar--bronze";
            VisualElement avatar = SocialStyle.Avatar(entry.Name, place == 1 ? "avatar--xl" : "avatar--lg");
            avatar.AddToClassList(medal);
            column.Add(avatar);

            var name = new Label(isMe ? entry.Name + " (tú)" : entry.Name);
            name.AddToClassList("podium__name");
            if (isMe) name.AddToClassList("podium__name--me");
            column.Add(name);

            var wins = new Label(entry.Wins.ToString());
            wins.AddToClassList("podium__wins");
            column.Add(wins);
            var label = new Label(entry.Wins == 1 ? "victoria" : "victorias");
            label.AddToClassList("podium__label");
            column.Add(label);

            var step = new VisualElement();
            step.AddToClassList("podium__step");
            step.AddToClassList("podium__step--" + place);
            var number = new Label(place.ToString());
            number.AddToClassList("podium__num");
            number.AddToClassList("podium__num--" + place);
            step.Add(number);
            column.Add(step);
            return column;
        }

        private static VisualElement BuildRow(int position, LeaderboardEntry entry, bool isMe)
        {
            var row = new VisualElement();
            row.AddToClassList("row-card");
            row.AddToClassList("lb-row");
            if (isMe) row.AddToClassList("row-card--accent");

            var pos = new Label(position.ToString());
            pos.AddToClassList("lb-pos");
            row.Add(pos);

            row.Add(SocialStyle.Avatar(entry.Name, "avatar--sm"));

            var info = new VisualElement();
            info.AddToClassList("row-card__info");
            var name = new Label(isMe ? entry.Name + " (tú)" : entry.Name);
            name.AddToClassList("row-card__name");
            info.Add(name);
            var sub = new Label(SocialStyle.Plural(entry.Losses, "derrota", "derrotas") + " · " + SocialStyle.WinRate(entry.Wins, entry.Losses) + "%");
            sub.AddToClassList("row-card__sub");
            info.Add(sub);
            row.Add(info);

            var wins = new VisualElement();
            wins.AddToClassList("lb-wins");
            var number = new Label(entry.Wins.ToString());
            number.AddToClassList("lb-wins__n");
            wins.Add(number);
            var text = new Label(entry.Wins == 1 ? "victoria" : "victorias");
            text.AddToClassList("lb-wins__t");
            wins.Add(text);
            row.Add(wins);
            return row;
        }

        private void SetMessage(string text, bool isError)
        {
            messageLabel.text = text;
            messageLabel.EnableInClassList("friends__msg--error", isError);
        }
    }
}
