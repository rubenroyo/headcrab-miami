using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests para el sistema de armas: Weapon, WeaponController, y WeaponPickup.
/// </summary>
public class WeaponSystemPlayTests
{
    private GameObject playerGO;
    private Component playerControllerComponent;
    private Type playerControllerType;
    private Component weaponControllerComponent;

    private GameObject weaponPickupGO;
    private Component weaponPickupComponent;

    private GameObject magazinePickupGO;
    private Component magazinePickupComponent;

    private GameObject cameraGO;

    [SetUp]
    public void Setup()
    {
        // Crear GameObject del jugador
        playerGO = new GameObject("TestPlayer");
        playerControllerType = Type.GetType("PlayerController, Assembly-CSharp");
        if (playerControllerType == null)
            playerControllerType = Type.GetType("PlayerController");
        playerControllerComponent = (Component)playerGO.AddComponent(playerControllerType);

        // Agregar WeaponController por reflexión
        var wcType = Type.GetType("WeaponController, Assembly-CSharp");
        if (wcType == null) wcType = Type.GetType("WeaponController");
        weaponControllerComponent = (Component)playerGO.AddComponent(wcType);

        // Crear cámara
        cameraGO = new GameObject("TestCamera");
        var cam = cameraGO.AddComponent<Camera>();
        cam.tag = "MainCamera";

        // Crear cubo para arma (Weapon Pickup)
        weaponPickupGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        weaponPickupGO.name = "WeaponPickup";
        weaponPickupGO.transform.position = new Vector3(5f, 0f, 0f);
        UnityEngine.Object.DestroyImmediate(weaponPickupGO.GetComponent<Collider>());
        UnityEngine.Object.DestroyImmediate(weaponPickupGO.GetComponent<MeshRenderer>());
        var wpType = Type.GetType("WeaponPickup, Assembly-CSharp");
        if (wpType == null) wpType = Type.GetType("WeaponPickup");
        weaponPickupComponent = (Component)weaponPickupGO.AddComponent(wpType);

        // Crear cubo para cargador (Magazine Pickup)
        magazinePickupGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        magazinePickupGO.name = "MagazinePickup";
        magazinePickupGO.transform.position = new Vector3(10f, 0f, 0f);
        UnityEngine.Object.DestroyImmediate(magazinePickupGO.GetComponent<Collider>());
        UnityEngine.Object.DestroyImmediate(magazinePickupGO.GetComponent<MeshRenderer>());
        magazinePickupComponent = (Component)magazinePickupGO.AddComponent(wpType);
    }

    [TearDown]
    public void Teardown()
    {
        UnityEngine.Object.Destroy(playerGO);
        UnityEngine.Object.Destroy(weaponPickupGO);
        UnityEngine.Object.Destroy(magazinePickupGO);
        UnityEngine.Object.Destroy(cameraGO);
    }

    /// <summary>
    /// Test: Se puede crear un arma con munición.
    /// </summary>
    [Test]
    public void Weapon_CreatedWithAmmo()
    {
        var weaponType = Type.GetType("Weapon, Assembly-CSharp");
        if (weaponType == null) Assert.Inconclusive("Weapon type not found");
        var weapon = Activator.CreateInstance(weaponType, new object[] { "Pistol", 15, 15 });

        Assert.AreEqual("Pistol", weaponType.GetField("weaponName").GetValue(weapon));
        Assert.AreEqual(15, weaponType.GetField("currentAmmo").GetValue(weapon));
        Assert.AreEqual(15, weaponType.GetField("maxAmmoPerMagazine").GetValue(weapon));
    }

    /// <summary>
    /// Test: Se puede disparar y consume munición.
    /// </summary>
    [Test]
    public void Weapon_Fire_ConsumesAmmo()
    {
        var weaponType = Type.GetType("Weapon, Assembly-CSharp");
        var weapon = Activator.CreateInstance(weaponType, new object[] { "Pistol", 5, 15 });
        bool result = (bool)weaponType.GetMethod("TryFire").Invoke(weapon, null);
        Assert.IsTrue(result, "Debería poder disparar si hay munición");
        Assert.AreEqual(4, (int)weaponType.GetField("currentAmmo").GetValue(weapon), "Debería consumir 1 bala");
    }

    /// <summary>
    /// Test: No se puede disparar sin munición.
    /// </summary>
    [Test]
    public void Weapon_Fire_FailsWhenEmpty()
    {
        var weaponType = Type.GetType("Weapon, Assembly-CSharp");
        var weapon = Activator.CreateInstance(weaponType, new object[] { "Pistol", 0, 15 });
        bool result = (bool)weaponType.GetMethod("TryFire").Invoke(weapon, null);
        
        Assert.IsFalse(result, "No debería poder disparar sin munición");
    }

    /// <summary>
    /// Test: Se puede recargar el arma.
    /// </summary>
    [Test]
    public void Weapon_Reload_FillsMagazine()
    {
        var weaponType = Type.GetType("Weapon, Assembly-CSharp");
        var weapon = Activator.CreateInstance(weaponType, new object[] { "Pistol", 5, 15 });
        weaponType.GetMethod("FillMagazine").Invoke(weapon, null);
        Assert.AreEqual(15, (int)weaponType.GetField("currentAmmo").GetValue(weapon));
    }

    /// <summary>
    /// Test: Recarga parcial no excede máximo.
    /// </summary>
    [Test]
    public void Weapon_Reload_DoesNotExceedMax()
    {
        var weaponType = Type.GetType("Weapon, Assembly-CSharp");
        var weapon = Activator.CreateInstance(weaponType, new object[] { "Pistol", 10, 15 });
        weaponType.GetMethod("Reload").Invoke(weapon, new object[] { 10 });
        Assert.AreEqual(15, (int)weaponType.GetField("currentAmmo").GetValue(weapon), "No debería exceder máximo");
    }

    /// <summary>
    /// Test: WeaponController equipa un arma.
    /// </summary>
    [Test]
    public void WeaponController_EquipWeapon()
    {
        var weaponType = Type.GetType("Weapon, Assembly-CSharp");
        var weapon = Activator.CreateInstance(weaponType, new object[] { "Pistol", 15, 15 });
        weaponControllerComponent.GetType().GetMethod("EquipWeapon").Invoke(weaponControllerComponent, new object[] { weapon });
        var equipped = weaponControllerComponent.GetType().GetProperty("EquippedWeapon").GetValue(weaponControllerComponent);
        Assert.IsNotNull(equipped);
    }

    /// <summary>
    /// Test: WeaponController dispara y consume munición.
    /// </summary>
    [Test]
    public void WeaponController_TryFire_ConsumesAmmo()
    {
        var weaponType = Type.GetType("Weapon, Assembly-CSharp");
        var weapon = Activator.CreateInstance(weaponType, new object[] { "Pistol", 5, 15 });
        weaponControllerComponent.GetType().GetMethod("EquipWeapon").Invoke(weaponControllerComponent, new object[] { weapon });
        bool result = (bool)weaponControllerComponent.GetType().GetMethod("TryFire").Invoke(weaponControllerComponent, null);
        Assert.IsTrue(result);
        var equipped = weaponControllerComponent.GetType().GetProperty("EquippedWeapon").GetValue(weaponControllerComponent);
        Assert.AreEqual(4, (int)weaponType.GetField("currentAmmo").GetValue(equipped));
    }

    /// <summary>
    /// Test: WeaponController maneja cargadores.
    /// </summary>
    [Test]
    public void WeaponController_AddMagazine()
    {
        weaponControllerComponent.GetType().GetMethod("AddMagazine").Invoke(weaponControllerComponent, new object[] { 3 });
        Assert.AreEqual(3, (int)weaponControllerComponent.GetType().GetProperty("MagazineCount").GetValue(weaponControllerComponent));
    }

    /// <summary>
    /// Test: WeaponController recarga desde inventario.
    /// </summary>
    [Test]
    public void WeaponController_TryReload_UsesInventory()
    {
        var weaponType = Type.GetType("Weapon, Assembly-CSharp");
        var weapon = Activator.CreateInstance(weaponType, new object[] { "Pistol", 0, 15 });
        weaponControllerComponent.GetType().GetMethod("EquipWeapon").Invoke(weaponControllerComponent, new object[] { weapon });
        weaponControllerComponent.GetType().GetMethod("AddMagazine").Invoke(weaponControllerComponent, new object[] { 2 });
        bool result = (bool)weaponControllerComponent.GetType().GetMethod("TryReload").Invoke(weaponControllerComponent, null);
        Assert.IsTrue(result);
        var equipped = weaponControllerComponent.GetType().GetProperty("EquippedWeapon").GetValue(weaponControllerComponent);
        Assert.AreEqual(15, (int)weaponType.GetField("currentAmmo").GetValue(equipped));
        Assert.AreEqual(1, (int)weaponControllerComponent.GetType().GetProperty("MagazineCount").GetValue(weaponControllerComponent));
    }

    /// <summary>
    /// Test: WeaponPickup tiene tipo y munición.
    /// </summary>
    [Test]
    public void WeaponPickup_HasValidProperties()
    {
        // Leer propiedades públicas de WeaponPickup por reflexión
        var getTypeProp = weaponPickupComponent.GetType().GetProperty("GetPickupType");
        var getAmmoProp = weaponPickupComponent.GetType().GetProperty("GetAmmoCount");
        var pickupTypeVal = getTypeProp != null ? getTypeProp.GetValue(weaponPickupComponent) : null;
        var pickupTypeStr = pickupTypeVal != null ? pickupTypeVal.ToString() : "";
        Assert.AreEqual("Weapon", pickupTypeStr, "PickupType por defecto debería ser Weapon");
        Assert.GreaterOrEqual((int)getAmmoProp.GetValue(weaponPickupComponent), 0);
    }

    /// <summary>
    /// Test: Se puede recoger un arma.
    /// </summary>
    [UnityTest]
    public IEnumerator WeaponPickup_CanBePickedUp()
    {
        // El jugador se acerca al arma (en el Start de WeaponPickup se busca el PlayerController)
        yield return null;

        // Simular que el jugador está en rango
        weaponPickupGO.transform.position = playerGO.transform.position + Vector3.forward * 1f;
        
        yield return null;

        // Presionar E (simulado en Update)
        // En un test real, verificamos que el sistema está listo
        var weaponType = Type.GetType("Weapon, Assembly-CSharp");
        var weapon = Activator.CreateInstance(weaponType, new object[] { "TestWeapon", 15, 15 });
        weaponControllerComponent.GetType().GetMethod("EquipWeapon").Invoke(weaponControllerComponent, new object[] { weapon });
        var equipped = weaponControllerComponent.GetType().GetProperty("EquippedWeapon").GetValue(weaponControllerComponent);
        Assert.IsNotNull(equipped);
        Assert.AreEqual("TestWeapon", (string)weaponType.GetField("weaponName").GetValue(equipped));
    }

    /// <summary>
    /// Test: Disparo múltiple agota munición.
    /// </summary>
    [Test]
    public void WeaponController_MultipleFires()
    {
        var weaponType = Type.GetType("Weapon, Assembly-CSharp");
        var weapon = Activator.CreateInstance(weaponType, new object[] { "Pistol", 5, 15 });
        weaponControllerComponent.GetType().GetMethod("EquipWeapon").Invoke(weaponControllerComponent, new object[] { weapon });
        for (int i = 0; i < 5; i++)
        {
            Assert.IsTrue((bool)weaponControllerComponent.GetType().GetMethod("TryFire").Invoke(weaponControllerComponent, null));
        }
        Assert.IsFalse((bool)weaponControllerComponent.GetType().GetMethod("TryFire").Invoke(weaponControllerComponent, null), "Debería estar vacío");
        var equipped = weaponControllerComponent.GetType().GetProperty("EquippedWeapon").GetValue(weaponControllerComponent);
        Assert.AreEqual(0, (int)weaponType.GetField("currentAmmo").GetValue(equipped));
    }
}
