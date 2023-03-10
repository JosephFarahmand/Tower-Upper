using Lindon.TowerUpper.Data;
using Lindon.TowerUpper.Profile;
using Lindon.UserManager.Base.Page;
using Lindon.UserManager.Page.Shop;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShopPage : UIPage
{
    private ProfileController m_ProfileController;

    [SerializeField] private BuyButton m_BuyButton;

    [Header("Preview")]
    [SerializeField] private PreviewDisplayer m_PreviewDisplayer;
    [SerializeField] private EventTrigger m_PreviewTrigger;

    private Dictionary<ItemCategory, List<ShopData>> m_ShopItems;

    [Header("Slot")]
    [SerializeField] private Transform m_SlotParent;
    [SerializeField] private ShopSlot m_SlotPrefab;
    private Dictionary<ItemCategory, List<ShopSlot>> m_ShopSlots;
    private List<ShopSlot> m_SlotsCollection;

    [Header("Tab")]
    [SerializeField] private TabGroup m_TabGroup;

    public class ShopData
    {
        public ShopData(ItemData item)
        {
            Item = item;
            State = ItemState.Lock;
        }

        public ItemData Item { get; private set; }
        public ItemState State { get; private set; }
        public int Id => Item.Id;
        public ItemCategory Category => Item.Category;

        public event Action<int, ItemCategory, ItemState> OnChangeState;

        public void ChangeState(ItemState newState)
        {
            State = newState;
            OnChangeState?.Invoke(Id, Category, State);
        }

        public enum ItemState
        {
            Lock,
            Unlock,
            Active
        }
    }

    protected override void SetValues()
    {
        m_PreviewDisplayer.Open();

        LoadProfile();

        SortSlots(ItemCategory.Skin);
    }

    protected override void SetValuesOnSceneLoad()
    {
        m_ProfileController = ProfileController.Instance;

        m_ProfileController.Profile.OnAddItem += Profile_OnAddItem;
        m_ProfileController.Profile.OnActiveItem += OnActiveItem;

        m_PreviewDisplayer ??= FindObjectOfType<PreviewDisplayer>();

        m_PreviewDisplayer.Setup(m_PreviewTrigger);

        LoadItems();
        GenerateSlots();
        GenerateTabs();
    }

    private void OnDisable()
    {
        m_PreviewDisplayer?.Close();
    }

    private void OnDestroy()
    {
        m_TabGroup.onTabChanged -= OnTabChanged;
        m_ProfileController.Profile.OnAddItem -= Profile_OnAddItem;
        m_ProfileController.Profile.OnActiveItem -= OnActiveItem;

        foreach (var slot in m_SlotsCollection)
        {
            slot.OnSlotSelectedHandler -= OnSelectSlot;
        }

        foreach (var items in m_ShopItems.Values)
        {
            foreach (var item in items)
            {
                item.OnChangeState -= OnChangeState;
            }
        }
    }

    /// <summary>
    /// Get data from game data and store in page
    /// </summary>
    private void LoadItems()
    {
        m_ShopItems = new Dictionary<ItemCategory, List<ShopData>>();
        var allItems = GameData.Instance.GetItems();
        foreach (var item in allItems)
        {
            var category = item.Category;
            var data = new ShopData(item);

            data.OnChangeState += OnChangeState;

            if (m_ShopItems.ContainsKey(category))
            {
                m_ShopItems[category].Add(data);
            }
            else
            {
                m_ShopItems.Add(category, new List<ShopData>() { data });
            }
        }
    }

    private void OnChangeState(int itemId, ItemCategory itemCategory, ShopData.ItemState itemState)
    {
        ShopSlot slot = GetSlot(itemId, itemCategory);
        if (slot == null) return;

        switch (itemState)
        {
            case ShopData.ItemState.Unlock:
                slot.SetUnlockSkin();
                break;
            case ShopData.ItemState.Active:
                slot.SetActiveSkin();
                break;
            case ShopData.ItemState.Lock:
                slot.SetLockSkin();
                break;
        }
    }

    /// <summary>
    /// Instantiate slot based on the data taken from game data
    /// </summary>
    private void GenerateSlots()
    {
        m_ShopSlots = new Dictionary<ItemCategory, List<ShopSlot>>();
        m_SlotsCollection = new List<ShopSlot>();

        foreach (var data in m_ShopItems)
        {
            var category = data.Key;
            var shopItems = data.Value;
            foreach (var shopItem in shopItems)
            {
                ItemData item = shopItem.Item;
                ShopSlot slotObj = Instantiate(m_SlotPrefab, m_SlotParent);
                slotObj.SetBuyButton(m_BuyButton);
                slotObj.SetItem(item);

                slotObj.SetLockSkin();

                slotObj.OnSlotSelectedHandler += OnSelectSlot;

                if (m_ShopSlots.ContainsKey(category))
                {
                    m_ShopSlots[category].Add(slotObj);
                }
                else
                {
                    m_ShopSlots.Add(category, new List<ShopSlot>() { slotObj });
                }

                m_SlotsCollection.Add(slotObj);
            }
        }
    }

    /// <summary>
    /// Tabs initialization based on item category
    /// </summary>
    private void GenerateTabs()
    {
        var tabQueue = new Queue<int>();
        foreach (var key in System.Enum.GetValues(typeof(ItemCategory)))
        {
            var category = (ItemCategory)key;
            if (category == ItemCategory.None) continue;
            tabQueue.Enqueue((int)category);
        }

        m_TabGroup.Initialization(tabQueue);
        m_TabGroup.onTabChanged += OnTabChanged;
    }

    private void OnTabChanged(int tabIndex)
    {
        var selectedTagCategory = (ItemCategory)tabIndex;
        foreach (var slotData in m_ShopSlots)
        {
            ItemCategory key = slotData.Key;
            List<ShopSlot> slots = slotData.Value;

            bool activeSlot = key == selectedTagCategory;
            SetActiveSlot(slots, activeSlot);
        }

        static void SetActiveSlot(List<ShopSlot> slots, bool value)
        {
            foreach (var slot in slots)
            {
                slot.gameObject.SetActive(value);
            }
            if (value)
            {
                slots[0].SelectSlot();
            }
        }

        SortSlots(selectedTagCategory);
    }

    /// <summary>
    /// Get items from profile and update stored data
    /// </summary>
    private void LoadProfile()
    {
        var profileItemIds = m_ProfileController.Profile.GetItems();

        foreach (var id in profileItemIds)
        {
            ShopData shopData = GetData(id);
            shopData.ChangeState(ShopData.ItemState.Unlock);
        }

        foreach (var key in System.Enum.GetValues(typeof(ItemCategory)))
        {
            var category = (ItemCategory)key;
            if (category == ItemCategory.None) continue;
            var id = m_ProfileController.Profile.GetActiveItem(category);
            if (id == -1) continue;
            ShopData shopData = GetData(id, category);
            shopData.ChangeState(ShopData.ItemState.Active);
        }
    }

    private void SortSlots(ItemCategory category)
    {
        if (!m_ShopItems.ContainsKey(category)) return;

        int index = -1;

        foreach (var items in m_ShopItems[category])
        {
            var itemState = items.State;
            var itemId = items.Id;
            var slot = GetSlot(itemId, category);

            switch (itemState)
            {
                case ShopData.ItemState.Lock:
                    slot.transform.SetAsLastSibling();
                    break;
                case ShopData.ItemState.Unlock:
                    slot.transform.SetSiblingIndex(1);
                    if (index == -1)
                    {
                        index = m_ShopSlots[category].IndexOf(slot);
                    }
                    break;
                case ShopData.ItemState.Active:
                    slot.transform.SetAsFirstSibling();
                    index = m_ShopSlots[category].IndexOf(slot);
                    break;
            }
        }

        if (index == -1)
        {
            index = 0;
        }
        ShopSlot selectedSlot = m_ShopSlots[category][index];
        selectedSlot.SelectSlot();
    }

    private void Profile_OnAddItem(int itemId)
    {
        ItemCategory category = GameData.Instance.GetItemByCategory(itemId);

        OnActiveItem(itemId, category);
    }

    private void OnActiveItem(int itemId, ItemCategory category)
    {
        foreach (ShopData shopItem in m_ShopItems[category])
        {
            if (shopItem.Id == itemId)
            {
                shopItem.ChangeState(ShopData.ItemState.Active);
            }
            else if (shopItem.State == ShopData.ItemState.Lock)
            {
                continue;
            }
            else
            {
                shopItem.ChangeState(ShopData.ItemState.Unlock);
            }
        }
    }

    private ShopData GetData(int itemId)
    {
        foreach (var items in m_ShopItems.Values)
        {
            foreach (var item in items)
            {
                if (item.Id == itemId)
                {
                    return item;
                }
            }
        }
        Debug.LogError($"Item with {itemId} was not found");
        return null;
    }

    private ShopData GetData(int itemId, ItemCategory category)
    {
        foreach (var shopData in m_ShopItems[category])
        {
            var item = shopData.Item;
            if (item.Id == itemId)
            {
                return shopData;
            }
        }
        Debug.LogError($"Item with {itemId} was not found");
        return null;
    }

    private ShopSlot GetSlot(int itemId, ItemCategory category)
    {
        foreach (var shopSlot in m_ShopSlots[category])
        {
            if (shopSlot.Id == itemId)
            {
                return shopSlot;
            }
        }
        Debug.LogError($"Item with {itemId} was not found");
        return null;
    }

    private void OnSelectSlot(bool isSelect, ItemData item)
    {
        if (isSelect)
        {
            int itemId = item.Id;
            ItemCategory category = item.Category;
            SetSlotSkin(itemId, category);
            SetActiveItem(itemId, category);
            DisplayItem(itemId);
        }
    }

    private void SetActiveItem(int itemId, ItemCategory category)
    {
        ShopData shopItem = GetData(itemId, category);
        if (shopItem.State == ShopData.ItemState.Unlock || shopItem.State == ShopData.ItemState.Active)
        {
            m_ProfileController.Profile.SetActiveItem(itemId, category);
        }
    }

    private void SetSlotSkin(int itemId, ItemCategory category)
    {
        ShopSlot slot = GetSlot(itemId, category);
        ShopData shopItem = GetData(itemId, category);
        if (shopItem == null || slot == null) return;

        ShopData.ItemState itemState = shopItem.State;
        switch (itemState)
        {
            case ShopData.ItemState.Unlock:
                slot.SetUnlockSkin();
                break;
            case ShopData.ItemState.Active:
                slot.SetActiveSkin();
                break;
            case ShopData.ItemState.Lock:
                slot.SetLockSkin();
                break;
        }
    }

    private void DisplayItem(int itemId)
    {
        m_PreviewDisplayer.Display(itemId);
    }
}
