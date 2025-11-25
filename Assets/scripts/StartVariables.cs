using UnityEngine;

public class StartVariables : MonoBehaviour
{
    private int tipoControle = 0;
    public GameObject Player;
    public GameObject Armas;

    void Awake()
    {
        tipoControle = PlayerPrefs.GetInt("tipoControle", 0);

        if (tipoControle == 0)
        {
            Player.GetComponent<MainCharacterController>().enabled = true;
            Player.GetComponent<MainCharacterControllerJoyCon>().enabled = false;

            foreach (WeaponController arma in Armas.GetComponentsInChildren<WeaponController>()){
                arma.enabled = true;
            }
            foreach (WeaponControllerJoyCon arma in Armas.GetComponentsInChildren<WeaponControllerJoyCon>()){
                arma.enabled = false;
            }
        }
        else
        {
            Player.GetComponent<MainCharacterController>().enabled = false;
            Player.GetComponent<MainCharacterControllerJoyCon>().enabled = true;

            foreach (WeaponController arma in Armas.GetComponentsInChildren<WeaponController>()){
                arma.enabled = false;
            }
            foreach (WeaponControllerJoyCon arma in Armas.GetComponentsInChildren<WeaponControllerJoyCon>()){
                arma.enabled = true;
            }
        }
    }

    void Start()
    {
        /*if (tipoControle == 0)
        {
            Player.GetComponent<MainCharacterController>().enabled = true;
            Player.GetComponent<MainCharacterControllerJoyCon>().enabled = false;

            foreach (WeaponController arma in Armas.GetComponentsInChildren<WeaponController>()){
                arma.enabled = true;
            }
            foreach (WeaponControllerJoyCon arma in Armas.GetComponentsInChildren<WeaponControllerJoyCon>()){
                arma.enabled = false;
            }
        }
        else
        {
            Player.GetComponent<MainCharacterController>().enabled = false;
            Player.GetComponent<MainCharacterControllerJoyCon>().enabled = true;

            foreach (WeaponController arma in Armas.GetComponentsInChildren<WeaponController>()){
                arma.enabled = false;
            }
            foreach (WeaponControllerJoyCon arma in Armas.GetComponentsInChildren<WeaponControllerJoyCon>()){
                arma.enabled = true;
            }
        }*/
    }

}
