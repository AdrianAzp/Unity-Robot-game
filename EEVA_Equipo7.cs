using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EEVA_Equipo7 : ISSR_TeamBehaviour {

    public override void CreateTeam()
    {
        if (!InitError())
        { //No hay error al inicializar
            if (RegisterTeam("EEVA", "Axiom Team"))
            { //Registro del equipo
                for (int index=0; index < GetNumberOfAgentsInTeam(); index++)
                    //Crear agentes en posiciones de marcadores
                    CreateAgent(new EEVA_Agente7());
            }
        }
    }
}