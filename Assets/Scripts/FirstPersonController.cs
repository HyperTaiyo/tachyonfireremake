using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private GameObject[] weapons;
    private int currentWeapon = 0;
    
    void Update()
    {
        // Weapon switching
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchWeapon(2);
    }
    
    void SwitchWeapon(int index)
    {
        if (index == currentWeapon) return;
        
        weapons[currentWeapon].SetActive(false);
        weapons[index].SetActive(true);
        currentWeapon = index;
    }
}