using MMORPG.Game;
using QFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using LightType = UnityEngine.LightType;

public class ItemController : MonoBehaviour, IController
{
    public EntityView EntityView;
    private static Dictionary<string, GameObject> _essenceDict;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_essenceDict == null)
        {
            _essenceDict = new();
            _essenceDict["usually"] = Resources.Load<GameObject>("Prefabs/Effect/Essence/ItemWhiteEssence");
            _essenceDict["extraordinary"] = Resources.Load<GameObject>("Prefabs/Effect/Essence/ItemGreenEssence");
            _essenceDict["rare"] = Resources.Load<GameObject>("Prefabs/Effect/Essence/ItemBlueEssence");
            _essenceDict["epic"] = Resources.Load<GameObject>("Prefabs/Effect/Essence/ItemPurpleEssence");
            _essenceDict["legend"] = Resources.Load<GameObject>("Prefabs/Effect/Essence/ItemOrangeEssence");
            _essenceDict["legend"] = Resources.Load<GameObject>("Prefabs/Effect/Essence/ItemRedEssence");
        }

        var pos = transform.position;
        pos.y = 20;
        transform.position = CalculateGroundPosition(pos, LayerMask.NameToLayer("Terrain"));

        // Find item quality
        var dataManagerSystem = this.GetSystem<IDataManagerSystem>();
        var itemDefine = dataManagerSystem.GetUnitItemDefine(EntityView.UnitDefine.ID);
        var essence = Instantiate(_essenceDict[itemDefine.Quality], transform);
        essence.transform.localScale = new Vector3(2, 2, 2);

        //Light pointLight = gameObject.AddComponent<Light>();
        //pointLight.type = LightType.Point;
        //pointLight.color = Color.white;
        //pointLight.intensity = 1f;
        //pointLight.range = 3f;
        //pointLight.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 CalculateGroundPosition(Vector3 position, int layer)
    {
        int layerMask = 1 << layer;
        if (Physics.Raycast(position, Vector3.down, out var hit, Mathf.Infinity, layerMask))
        {
            return hit.point;
        }
        return position;
    }

    public IArchitecture GetArchitecture()
    {
        return GameApp.Interface;
    }
}
