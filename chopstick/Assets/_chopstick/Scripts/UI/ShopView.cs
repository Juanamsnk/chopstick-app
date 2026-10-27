using System.Collections.Generic;
using Chopsticks.Career;
using Chopsticks.Shop;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// La tienda: monedas siempre visibles, tono de piel en una fila pequena y los estilos
    /// en una cuadricula de tarjetas cuadradas (tocar la tarjeta compra o equipa).
    /// </summary>
    public class ShopView
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
            public VisualElement StatusCoin;
            public Label StatusText;
        }

        private static readonly Color[] RarityColors =
        {
            new Color32(0xd6, 0xd1, 0xf5, 255),
            new Color32(0x3f, 0xa9, 0xff, 255),
            new Color32(0xb7, 0x9b, 0xff, 255),
            new Color32(0xff, 0xc8, 0x3d, 255)
        };

        private static bool warnedMissing;

        private readonly ShopInventory inventory;
        private readonly Label coinsLabel;
        private readonly VisualElement tonesRow;
        private readonly VisualElement legend;
        private readonly ScrollView scroll;
        private readonly Button[] toneButtons = new Button[SkinTones.Count];
        private readonly List<Card> cards = new List<Card>();

        // Ventana de detalle (se abre al tocar una tarjeta)
        private readonly VisualElement detailOverlay;
        private readonly VisualElement detailBox;
        private readonly Label detailRarity;
        private readonly Label detailName;
        private readonly Label detailDesc;
        private readonly Label detailNote;
        private readonly Button detailBuy;
        private readonly Card detailCard = new Card();
        private HandSkin detailSkin;
        private bool detailJustBought;

        public ShopView(VisualElement screen, ShopInventory inventory)
        {
            this.inventory = inventory;
            coinsLabel = screen.Q<Label>("shop-coins");
            tonesRow = screen.Q("tones");
            legend = screen.Q("legend");
            scroll = screen.Q<ScrollView>("skins");

            // Esta pantalla solo necesita el boton de volver
            Button primary = screen.Q("action-row").Q<Button>("btn-primary");
            if (primary != null) primary.style.display = DisplayStyle.None;

            // El texto de ayuda sale de los niveles reales, asi no hay que cambiarlo a mano
            Label hint = screen.Q<Label>(className: "shop__hint");
            if (hint != null)
            {
                hint.text = "Las monedas se consiguen en el Modo carrera, que también desbloquea estilos: raras en el nivel " +
                            HandSkinCatalog.UnlockLevelOf(SkinRarity.Rara) + ", épicas en el " +
                            HandSkinCatalog.UnlockLevelOf(SkinRarity.Epica) + " y legendarias en el " +
                            HandSkinCatalog.UnlockLevelOf(SkinRarity.Legendaria) + ".";
            }

            BuildLegend();
            BuildTones();
            BuildCards();

            detailOverlay = screen.Q("skin-detail");
            detailBox = screen.Q("detail-box");
            detailRarity = screen.Q<Label>("detail-rarity");
            detailName = screen.Q<Label>("detail-name");
            detailDesc = screen.Q<Label>("detail-desc");
            detailNote = screen.Q<Label>("detail-note");
            detailBuy = screen.Q<Button>("btn-detail-buy");
            BuildDetail(screen);

            Refresh();
        }

        // ------------------------------------------------------------
        // Construir
        // ------------------------------------------------------------
        private void BuildLegend()
        {
            for (int r = 0; r < 4; r++)
            {
                var rarity = (SkinRarity)r;
                var item = new VisualElement();
                item.AddToClassList("legend-item");

                var dot = new VisualElement();
                dot.AddToClassList("legend-item__dot");
                dot.style.backgroundColor = RarityColors[r];
                item.Add(dot);

                var text = new Label(HandSkinCatalog.RarityName(rarity) + " " + HandSkinCatalog.PriceOf(rarity));
                text.AddToClassList("legend-item__text");
                item.Add(text);

                legend.Add(item);
            }
        }

        private void BuildTones()
        {
            for (int i = 0; i < SkinTones.Count; i++)
            {
                int tone = i;
                var button = new Button();
                button.AddToClassList("tone-btn");
                button.style.backgroundColor = SkinTones.Fill(i);
                button.clicked += () =>
                {
                    inventory.SetTone(tone);
                    Refresh();
                };
                tonesRow.Add(button);
                toneButtons[i] = button;
            }
        }

        private void BuildCards()
        {
            // Una sola cuadricula, ordenada de comun a legendaria (como en el catalogo)
            var grid = new VisualElement();
            grid.AddToClassList("shop__grid");
            scroll.Add(grid);

            for (int r = 0; r < 4; r++)
            {
                foreach (HandSkin skin in HandSkinCatalog.All)
                {
                    if ((int)skin.Rarity != r) continue;
                    Card card = BuildCard(skin);
                    grid.Add(card.Root);
                    cards.Add(card);
                }
            }
        }

        private Card BuildCard(HandSkin skin)
        {
            var card = new Card { Skin = skin };

            card.Root = new VisualElement();
            card.Root.AddToClassList("skin-card");
            card.Root.RegisterCallback<ClickEvent>(evt => ShowDetail(skin));

            // Zona de la mano: siempre cuadrada
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
            card.StatusCoin = new VisualElement();
            card.StatusCoin.AddToClassList("skin-card__status-coin");
            card.StatusText = new Label();
            card.StatusText.AddToClassList("skin-card__status-text");
            card.Status.Add(card.StatusCoin);
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
        // Acciones y repintado
        // ------------------------------------------------------------
        // ------------------------------------------------------------
        // Ventana de detalle
        // ------------------------------------------------------------
        private void BuildDetail(VisualElement screen)
        {
            // El dibujo grande: PNG si existe, mano de respaldo si no (igual que en las tarjetas)
            detailCard.Preview = screen.Q("detail-preview");
            detailCard.Sprite = new Image { scaleMode = ScaleMode.ScaleToFit };
            detailCard.Sprite.AddToClassList("skin-detail__sprite");
            detailCard.Sprite.pickingMode = PickingMode.Ignore;
            detailCard.Preview.Add(detailCard.Sprite);
            BuildFallback(detailCard);

            detailBuy.clicked += OnDetailAction;
            screen.Q<Button>("btn-detail-close").clicked += CloseDetail;

            // Tocar fuera de la caja (en lo oscuro) tambien cierra
            detailOverlay.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == detailOverlay) CloseDetail();
            });
        }

        private void ShowDetail(HandSkin skin)
        {
            detailSkin = skin;
            detailJustBought = false;
            detailOverlay.style.display = DisplayStyle.Flex;
            RefreshDetail();
        }

        /// <summary>Cierra la ventana de detalle (si estaba abierta).</summary>
        public void CloseDetail()
        {
            detailSkin = null;
            detailOverlay.style.display = DisplayStyle.None;
        }

        private void OnDetailAction()
        {
            HandSkin skin = detailSkin;
            if (skin == null) return;

            if (!inventory.Owns(skin.Id))
            {
                if (inventory.TryBuy(skin)) detailJustBought = true;   // comprar tambien lo equipa
            }
            else
            {
                inventory.Equip(skin.Id);
            }
            Refresh();
        }

        private void RefreshDetail()
        {
            HandSkin skin = detailSkin;
            if (skin == null) return;

            bool owned = inventory.Owns(skin.Id);
            bool equipped = inventory.EquippedId == skin.Id;
            bool canBuy = inventory.CanBuy(skin);

            detailCard.Skin = skin;
            ApplyPreview(detailCard, skin);

            Color rarityColor = RarityColors[(int)skin.Rarity];
            detailBox.style.borderTopColor = rarityColor;
            detailBox.style.borderBottomColor = rarityColor;
            detailBox.style.borderLeftColor = rarityColor;
            detailBox.style.borderRightColor = rarityColor;
            detailRarity.style.color = rarityColor;
            detailRarity.text = skin.Id == HandSkinCatalog.DefaultId
                ? "Gratis"
                : HandSkinCatalog.RarityName(skin.Rarity) + " · " + skin.Price + " monedas";

            detailName.text = skin.Name;
            detailDesc.text = skin.Description;

            if (equipped)
            {
                detailNote.text = detailJustBought ? "¡Comprada! Ya la llevas puesta." : "La llevas puesta ahora.";
                detailBuy.text = "Equipada";
                detailBuy.SetEnabled(false);
                detailBuy.style.display = DisplayStyle.Flex;
            }
            else if (owned)
            {
                detailNote.text = "Ya es tuya.";
                detailBuy.text = "Equipar";
                detailBuy.SetEnabled(true);
                detailBuy.style.display = DisplayStyle.Flex;
            }
            else if (!inventory.MeetsLevel(skin))
            {
                // Bloqueado por la carrera: se explica que nivel hace falta y cuanto llevas
                detailNote.text = "Supera el nivel " + skin.UnlockLevel + " del Modo carrera (rango " +
                                  CareerLevels.TierName(skin.UnlockLevel) + ") para poder comprarlo. Llevas " +
                                  inventory.CareerLevel + " niveles superados.";
                detailBuy.style.display = DisplayStyle.None;
            }
            else if (canBuy)
            {
                detailNote.text = "Tienes " + inventory.Coins + " monedas.";
                detailBuy.text = "Confirmar compra (" + skin.Price + ")";
                detailBuy.SetEnabled(true);
                detailBuy.style.display = DisplayStyle.Flex;
            }
            else
            {
                // Sin monedas suficientes: no hay boton de compra, solo se explica cuanto falta
                detailNote.text = "Te faltan " + (skin.Price - inventory.Coins) + " monedas. Las consigues superando niveles del Modo carrera.";
                detailBuy.style.display = DisplayStyle.None;
            }
        }

        /// <summary>Repinta monedas, tonos, dibujos y estados. No reconstruye la lista, asi el scroll no salta.</summary>
        public void Refresh()
        {
            coinsLabel.text = inventory.Coins.ToString();

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

            foreach (Card card in cards) RefreshCard(card);
            RefreshDetail();
        }

        private void RefreshCard(Card card)
        {
            HandSkin skin = card.Skin;
            bool owned = inventory.Owns(skin.Id);
            bool equipped = inventory.EquippedId == skin.Id;
            bool canBuy = inventory.CanBuy(skin);
            bool levelLocked = !owned && !inventory.MeetsLevel(skin);

            ApplyPreview(card, skin);

            card.Root.EnableInClassList("skin-card--equipped", equipped);
            card.Root.EnableInClassList("skin-card--locked", !owned && !canBuy);
            card.Root.EnableInClassList("skin-card--levellocked", levelLocked);

            card.Status.EnableInClassList("skin-card__status--equipped", equipped);
            card.Status.EnableInClassList("skin-card__status--owned", owned && !equipped);
            card.Status.EnableInClassList("skin-card__status--locked", !owned && !canBuy);
            card.StatusCoin.style.display = owned || levelLocked ? DisplayStyle.None : DisplayStyle.Flex;

            if (equipped) card.StatusText.text = "Equipada";
            else if (owned) card.StatusText.text = "Equipar";
            else if (levelLocked) card.StatusText.text = "Nivel " + skin.UnlockLevel;
            else card.StatusText.text = skin.Price.ToString();
        }

        /// <summary>Pone el dibujo de la mano: PNG si existe, mano de respaldo si no.</summary>
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
                if (!warnedMissing)
                {
                    warnedMissing = true;
                    Debug.LogWarning("[Tienda] No se encuentra el PNG de las manos. Debe estar en una carpeta Resources/HandSkins " +
                                     "(ej: Resources/HandSkins/clasica_t0_4.png, Resources/HandSkins/zombi_4.png).");
                }
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
