using System;
using System.Collections.Generic;
using Chopsticks.Career;
using Chopsticks.Shop;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// El inventario: todas las manos que tienes, compradas en la tienda o ganadas en la carrera.
    /// Tocar una mano que tienes la equipa. Las manos de carrera que aun no has ganado salen apagadas,
    /// y al tocarlas te dice que rango hay que vencer para conseguirlas.
    /// </summary>
    public class InventoryView
    {
        private class Card
        {
            public HandSkin Skin;
            public VisualElement Root;
            public VisualElement Preview;
            public Image Sprite;
            public VisualElement Fallback;
            public VisualElement FallbackPalm;
            public List<VisualElement> FallbackFingers = new List<VisualElement>();
            public VisualElement Status;
            public Label StatusText;
        }

        private static readonly Color[] RarityColors =
        {
            new Color32(0xd6, 0xd1, 0xf5, 255),
            new Color32(0x3f, 0xa9, 0xff, 255),
            new Color32(0xb7, 0x9b, 0xff, 255),
            new Color32(0xff, 0xc8, 0x3d, 255)
        };

        private const string DefaultMessage = "Toca una mano para equiparla.";

        private readonly ShopInventory inventory;
        private readonly Label countLabel;
        private readonly Label messageLabel;
        private readonly VisualElement tonesRow;
        private readonly VisualElement grid;
        private string orderKey = "";   // que manos se ven y en que orden (solo se recoloca si cambia)
        private readonly Button[] toneButtons = new Button[SkinTones.Count];
        private readonly List<Card> cards = new List<Card>();

        public event Action ShopClicked;
        public event Action BackClicked;

        public InventoryView(VisualElement screen, ShopInventory inventory)
        {
            this.inventory = inventory;
            countLabel = screen.Q<Label>("inv-count");
            messageLabel = screen.Q<Label>("inv-msg");
            tonesRow = screen.Q("inv-tones");
            ScrollView scroll = screen.Q<ScrollView>("inv-scroll");

            // Abajo: "Tienda" (para conseguir mas manos) y "Volver" (a donde estabas)
            VisualElement row = screen.Q("action-row");
            Button primary = row.Q<Button>("btn-primary");
            primary.text = "Tienda";
            primary.AddToClassList("btn--blue");
            primary.clicked += () => { if (ShopClicked != null) ShopClicked(); };
            row.Q<Button>("btn-secondary").clicked += () => { if (BackClicked != null) BackClicked(); };

            BuildTones();

            // Una sola cuadricula: primero las manos que tienes y al final las de carrera que aun no has ganado (apagadas)
            grid = new VisualElement();
            grid.AddToClassList("shop__grid");
            scroll.Add(grid);

            foreach (HandSkin skin in HandSkinCatalog.All) cards.Add(BuildCard(skin));

            messageLabel.text = DefaultMessage;
            Refresh();
        }

        // ------------------------------------------------------------
        // Construir
        // ------------------------------------------------------------
        private void BuildTones()
        {
            for (int i = 0; i < SkinTones.Count; i++)
            {
                int tone = i;
                var button = new Button();
                button.AddToClassList("tone-btn");
                button.style.backgroundColor = SkinTones.Fill(i);
                button.clicked += () => inventory.SetTone(tone);
                tonesRow.Add(button);
                toneButtons[i] = button;
            }
        }

        private Card BuildCard(HandSkin skin)
        {
            var card = new Card { Skin = skin };

            card.Root = new VisualElement();
            card.Root.AddToClassList("skin-card");
            card.Root.RegisterCallback<ClickEvent>(evt => OnCardClicked(card));

            card.Preview = new VisualElement();
            card.Preview.AddToClassList("skin-card__preview");
            card.Preview.pickingMode = PickingMode.Ignore;
            card.Preview.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                float w = card.Preview.resolvedStyle.width;
                if (w > 1f && Mathf.Abs(card.Preview.resolvedStyle.height - w) > 0.5f) card.Preview.style.height = w;
            });
            card.Root.Add(card.Preview);

            card.Sprite = new Image { scaleMode = ScaleMode.ScaleToFit };
            card.Sprite.AddToClassList("skin-card__sprite");
            card.Sprite.pickingMode = PickingMode.Ignore;
            card.Preview.Add(card.Sprite);

            BuildFallback(card);

            var rarityDot = new VisualElement();
            rarityDot.AddToClassList("skin-card__rarity");
            rarityDot.style.backgroundColor = RarityColors[(int)skin.Rarity];
            rarityDot.pickingMode = PickingMode.Ignore;
            card.Preview.Add(rarityDot);

            var name = new Label(skin.Name);
            name.AddToClassList("skin-card__name");
            name.pickingMode = PickingMode.Ignore;
            card.Root.Add(name);

            var desc = new Label(skin.Description);
            desc.AddToClassList("skin-card__desc");
            desc.pickingMode = PickingMode.Ignore;
            card.Root.Add(desc);

            card.Status = new VisualElement();
            card.Status.AddToClassList("skin-card__status");
            card.Status.pickingMode = PickingMode.Ignore;
            card.StatusText = new Label();
            card.StatusText.AddToClassList("skin-card__status-text");
            card.Status.Add(card.StatusText);
            card.Root.Add(card.Status);

            return card;
        }

        /// <summary>Mano sencilla de cuatro dedos que se ve solo si falta el PNG.</summary>
        private void BuildFallback(Card card)
        {
            card.Fallback = new VisualElement();
            card.Fallback.AddToClassList("mini-hand");
            card.Fallback.pickingMode = PickingMode.Ignore;

            var fingers = new VisualElement();
            fingers.AddToClassList("mini-hand__fingers");
            float[] heights = { 0.75f, 1f, 0.95f, 0.8f };
            for (int i = 0; i < 4; i++)
            {
                var finger = new VisualElement();
                finger.AddToClassList("mini-hand__finger");
                finger.style.height = Length.Percent(heights[i] * 100f);
                fingers.Add(finger);
                card.FallbackFingers.Add(finger);
            }
            card.Fallback.Add(fingers);

            card.FallbackPalm = new VisualElement();
            card.FallbackPalm.AddToClassList("mini-hand__palm");
            card.Fallback.Add(card.FallbackPalm);

            card.Preview.Add(card.Fallback);
        }

        // ------------------------------------------------------------
        // Acciones
        // ------------------------------------------------------------
        private void OnCardClicked(Card card)
        {
            HandSkin skin = card.Skin;

            if (inventory.Owns(skin.Id))
            {
                inventory.Equip(skin.Id);   // avisa con Changed y se repinta sola
                messageLabel.text = "Llevas puesta: " + skin.Name + ".";
                return;
            }

            // Una mano de carrera que aun no tienes: se explica como conseguirla
            int level = skin.CareerRewardLevel;
            int tier = CareerOpponentSkins.TierOf(skin.Id);
            if (level > 0 && tier >= 0)
            {
                messageLabel.text = "Vence el nivel " + level + " del Modo carrera (rango " +
                                    CareerLevels.TierNameByIndex(tier) + ") para ganar la mano " + skin.Name +
                                    ". Llevas " + inventory.CompletedLevels + " niveles superados.";
            }
        }

        // ------------------------------------------------------------
        // Repintado
        // ------------------------------------------------------------
        public void Refresh()
        {
            int total = HandSkinCatalog.All.Count;
            int have = 0;

            for (int i = 0; i < toneButtons.Length; i++)
            {
                bool on = i == inventory.Tone;
                toneButtons[i].EnableInClassList("tone-btn--on", on);
                Color line = on ? new Color32(0xff, 0xc8, 0x3d, 255) : SkinTones.Line(i);
                toneButtons[i].style.borderTopColor = line;
                toneButtons[i].style.borderBottomColor = line;
                toneButtons[i].style.borderLeftColor = line;
                toneButtons[i].style.borderRightColor = line;
            }

            // Orden: las que tienes (por rareza y, dentro de cada rareza, las de tienda y luego las de carrera por nivel)
            // y despues las de carrera que faltan, de menor a mayor nivel. Las de la tienda que no tienes no salen.
            var shown = new List<Card>();
            var missing = new List<Card>();
            foreach (Card card in cards)
            {
                if (inventory.Owns(card.Skin.Id)) shown.Add(card);
                else if (card.Skin.Source == HandSkinSource.Career) missing.Add(card);
            }
            shown.Sort(CompareOwned);
            missing.Sort(CompareMissing);
            shown.AddRange(missing);

            string key = "";
            foreach (Card card in shown) key += card.Skin.Id + ",";
            if (key != orderKey)
            {
                orderKey = key;
                grid.Clear();
                foreach (Card card in shown) grid.Add(card.Root);
            }

            foreach (Card card in cards)
            {
                HandSkin skin = card.Skin;
                bool owned = inventory.Owns(skin.Id);
                bool equipped = inventory.EquippedId == skin.Id;
                if (owned) have++;

                ApplyPreview(card, skin);
                card.Root.EnableInClassList("skin-card--equipped", equipped);
                card.Root.EnableInClassList("skin-card--levellocked", !owned);

                card.Status.EnableInClassList("skin-card__status--equipped", equipped);
                card.Status.EnableInClassList("skin-card__status--owned", owned && !equipped);
                card.Status.EnableInClassList("skin-card__status--locked", !owned);

                if (equipped) card.StatusText.text = "Equipada";
                else if (owned) card.StatusText.text = "Equipar";
                else card.StatusText.text = "Nivel " + skin.CareerRewardLevel;
            }

            countLabel.text = have + " de " + total + " manos";
        }

        private static int CatalogIndex(HandSkin skin)
        {
            for (int i = 0; i < HandSkinCatalog.All.Count; i++)
            {
                if (HandSkinCatalog.All[i].Id == skin.Id) return i;
            }
            return int.MaxValue;
        }

        private static int CompareOwned(Card a, Card b)
        {
            int result = ((int)a.Skin.Rarity).CompareTo((int)b.Skin.Rarity);
            if (result != 0) return result;
            result = a.Skin.CareerRewardLevel.CompareTo(b.Skin.CareerRewardLevel);   // las de tienda (0) van antes que las de carrera
            if (result != 0) return result;
            return CatalogIndex(a.Skin).CompareTo(CatalogIndex(b.Skin));
        }

        private static int CompareMissing(Card a, Card b)
        {
            int result = a.Skin.CareerRewardLevel.CompareTo(b.Skin.CareerRewardLevel);
            if (result != 0) return result;
            return CatalogIndex(a.Skin).CompareTo(CatalogIndex(b.Skin));
        }

        private void ApplyPreview(Card card, HandSkin skin)
        {
            Texture2D texture = HandSkinSprites.Get(skin.Id, inventory.Tone, 4);
            if (texture != null)
            {
                card.Sprite.image = texture;
                card.Sprite.style.display = DisplayStyle.Flex;
                card.Fallback.style.display = DisplayStyle.None;
            }
            else
            {
                card.Sprite.style.display = DisplayStyle.None;
                card.Fallback.style.display = DisplayStyle.Flex;
                PaintFallback(card);
            }
        }

        private void PaintFallback(Card card)
        {
            Color fill, line;
            if (card.Skin.UsesSkinTone)
            {
                fill = SkinTones.Fill(inventory.Tone);
                line = SkinTones.Line(inventory.Tone);
            }
            else
            {
                fill = RarityColors[(int)card.Skin.Rarity];
                line = fill * 0.7f;
                line.a = 1f;
            }

            foreach (VisualElement f in card.FallbackFingers) Paint(f, fill, line);
            Paint(card.FallbackPalm, fill, line);
        }

        private static void Paint(VisualElement e, Color fill, Color line)
        {
            e.style.backgroundColor = fill;
            e.style.borderTopColor = line;
            e.style.borderBottomColor = line;
            e.style.borderLeftColor = line;
            e.style.borderRightColor = line;
        }
    }
}
