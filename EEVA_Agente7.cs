using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EEVA_Agente7 : ISSR_Agent {

    //Tiene sentido tener un SF_Error para que si un agente se quede en error (por lo que sea) en el siguiente onTickElapsed vuelva a Idle??
    //Con un poco de suerte si el error ya n uelve a ocurrir hemos recuperado a ese agente

    enum EEVA_MsgCode
    {
        AvailableStone,
        NonAvailableStone,
        LetsGoToGoal,
        ExploredLocation,
        StoneSelected,
        EveryoneIsHere,
        GetOutMyWay
        //Codigo de mensaje voy a coger una piedra pequeña (puede que tambien otro para las grandes) y distancia a la misma?? Asi podriamos 
        //hacer que nuestros agentes calculasen quien esta mas cerca de la piedra y que solo ese agente fuese a por la piedra -> evitamos colisiones cerca de la meta
        //Podemos tener el problema de que haya una diferencia de tiempo suiciente grande para que al calcular la ditancia de mi agente a la piedra esta sea menor 
        //que la había dicho el otro agente al enviar el mensaje pero que sea yo el que estoy más lejos
        //DONDE METO LAS PIEDRAS QUE VEO QUE YA ESTAN SELECCIONADAS POR OTROS AGENTES?????
    }

	// Use this for initialization
	public override void Start () {

        ISSRHelp.SetupScoutingLocations(this);
        Debug.LogFormat("{0}: comienza", Myself.Name);
        current_state = ISSRState.GoingToMeetingPoint;
        acGotoLocation(iMyGoalLocation());
        if (acCheckError())
            current_state = ISSRState.Error;

	}
	
	// Update is called once per frame
	public override IEnumerator Update () {

        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            //En este punto generaremos un evento de tiempo
            current_event = ISSREventType.onTickElapsed;
            if (current_state != ISSRState.Scouting)
                ISSRHelp.UpdateVisitedScoutingLocation(this);
            current_state = AgentStateMachine();
            Share();
        }
		
	}

    /*** ---------------------------------------------------------------
                          Funciones de eventos
    ----------------------------------------------------------------***/

    public override void onEnterSensingArea(ISSR_Object obj) //Comienzo a ver objeto obj
    {
        object_just_seen = obj; //Anotamos objeto que vemos
        if (obj.type == ISSR_Type.SmallStone) //Si es una piedra pequeña
        {
            if (oiGrippingAgents(obj) > 0)
                SStoneIsAvailable(obj, false);
            else
                SStoneIsAvailable(obj, true);
        }
        if (obj.type == ISSR_Type.BigStone) //Si es una piedra grande
        {
            if (oiGrippingAgents(obj) > 1)
                BStoneIsAvailable(obj, false);
            else
                BStoneIsAvailable(obj, true);
        }
        current_state = AgentStateMachine();
    }

    public override void onGripSuccess(ISSR_Object obj_gripped) //Objeto agarrado por el agente
    {
        Debug.LogFormat("{0}: agarra '{1}'", Myself.Name, obj_gripped.Name);
        current_state = AgentStateMachine(); //Llamar a la máquina de estados
    }

    public override void onGObjectScored(ISSR_Object stone_that_scored) //Acabo de meter una piedra en la meta
    {
        Debug.LogFormat("{0}: piedra '{1}', metida en meta", Myself.Name, stone_that_scored.Name);
        current_state = AgentStateMachine(); //Llamar a la máquina de estados
    }

    public override void onCollision(ISSR_Object obj_that_collided_with_me)
    {
        colliding_object = obj_that_collided_with_me; //guardo el objeto con el que he chocado
        current_state = AgentStateMachine(); //actualizo el estado
    }


    public override void onDestArrived()
    {
        current_state = AgentStateMachine(); //actualizo mi estado
    }

    public override void onGObjectCollision(ISSR_Object obj_that_collided_with_gripped_obj)
    {
        colliding_object = obj_that_collided_with_gripped_obj; //anoto el objecto con el que ha chocado mi piedra
        current_state = AgentStateMachine(); //actualizo el estado
    }

    public override void onGripFailure(ISSR_Object obj_I_wanted_to_grip)
    {
        current_state = AgentStateMachine();
    }

    public override void onObjectLost(ISSR_Object obj_i_was_looking_for)
    {
        current_state = AgentStateMachine();
    }

    public override void onUngrip(ISSR_Object ungripped_object)
    {
        current_state = AgentStateMachine();
    }

    public override void onManyCollisions()
    {
        current_state = AgentStateMachine();
    }

    public override void onMsgArrived(ISSR_Message msg)
    {
        ProcessMessage(msg);
    }

    public override void onTimerOut(float delay)
    {
        current_state = AgentStateMachine();
    }

    public override void onPushTimeOut(ISSR_Object gripped_big_stone)
    {
        current_state = AgentStateMachine();
    }

    public override void onAnotherAgentGripped(ISSR_Object agent)
    {
        current_state = AgentStateMachine();
    }

    public override void onAnotherAgentUngripped(ISSR_Object agent)
    {
        current_state = AgentStateMachine();
    }

    public override void onStop()
    {
        current_state = AgentStateMachine();
    }

    /*** ---------------------------------------------------------------
                     Función de máquina de estados
    ----------------------------------------------------------------***/

    ISSRState AgentStateMachine() //Función principal de la máquina de estados
    {
        ISSRState next_state = current_state; //estado de salida, en principio igual

        switch (current_state) //según el estado
        {
            case ISSRState.Idle:
                next_state = SF_Idle();
                break;
            case ISSRState.GoingToGripSmallStone:
                next_state = SF_GoingToGripSmallStone();
                break;
            case ISSRState.GoingToGoalWithSmallStone:
                next_state = SF_GoingToGoalWithSmallStone();
                break;
            case ISSRState.AvoidingObstacle:
                next_state = SF_AvoidingObstacle();
                break;
            case ISSRState.WaitforNoStonesMoving:
                next_state = SF_WaitforNoStonesMoving();
                break;
            case ISSRState.SleepingAfterCollisions:
                next_state = SF_SleepingAfterCollisions();
                break;
            case ISSRState.GoingToGripBigStone:
                next_state = SF_GoingToGripBigStone();
                break;
            case ISSRState.WaitingForHelpToMoveBigStone:
                next_state = SF_WaitingForHelpToMoveBigStone();
                break;
            case ISSRState.WaitforNoStonesMovingBigStone:
                next_state = SF_WaitforNoStonesMovingBigStone();
                break;
            case ISSRState.GoingToGoalWithBigStone:
                next_state = SF_GoingToGoalWithBigStone();
                break;
            case ISSRState.Scouting:
                next_state = SF_Scouting();
                break;
            case ISSRState.GoingToMeetingPoint:
                next_state = SF_GoingToMeetingPoint();
                break;
            case ISSRState.WaitingForPartners:
                next_state = SF_WaitingForPartners();
                break;
            case ISSRState.GettingOutOfTheWay:
                next_state = SF_GettingOutOfTheWay();
                break;
            case ISSRState.White:
                next_state = SF_White();
                break;
            case ISSRState.Error:
                next_state = SF_Error();
                break;
            case ISSRState.End:
                break;
            default:
                Debug.LogFormat("{0}: estado {1} no considerado", Myself.Name, current_state);
                break;
        }

        if (current_state != next_state) //si ha cambiado el estado
        {
            Debug.LogWarningFormat("{0}: Estado '{1}' --> '{2}' por evento {3}", Myself.Name, current_state, next_state, current_event);
        }

        return next_state;
    }

    /*** ---------------------------------------------------------------
                        Funciones de  los estados 
    ----------------------------------------------------------------***/

    ISSRState SF_Error()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            //Si un agente se queda en estado de error en el siguiente evento onTickElapsed 
            //lo volvemos a poner en el estado Idle para intentar "reiniciarle"
            case ISSREventType.onTickElapsed: 
                next_state = ISSRState.Idle;
                break;
        }

        return next_state;
    }

    ISSRState SF_Idle() //SF "State Function"
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onTickElapsed:
                //coger de lista objeto más cercano a agente, se convierte en el objeto de interés
                focus_object = ISSRHelp.Get_next_available_stone_closer_to_me(this);

                if (focus_object != null) //Si hay alguno (focus_object está definido)
                {
                    if (focus_object.type == ISSR_Type.SmallStone)
                        next_state = GetSStone(focus_object);
                    if (focus_object.type == ISSR_Type.BigStone)
                        next_state = GetBStone(focus_object);
                }
                else
                {
                    int remain;
                    //Tambien podemos usar GetColserToGoalLocationInList()
                    //Prefiero usar GetColserToMeLocationInList porque como vamos a empezar yendo a la meta siempre vamos a coger
                    //piedras de dentro a fuera pero, depende de por donde estén los agentes, no todos tienen que ir a por la misma 
                    //piedra (no como pasaria con GetColserToGoalLocationInList)
                    focus_location = ISSRHelp.GetCloserToMeLocationInList(this, Valid_Locations, out remain);
                    if (remain > 0)
                    {
                        acGotoLocation(focus_location);
                        if (acCheckError())
                            next_state = ISSRState.Error;
                        else
                            next_state = ISSRState.Scouting;
                    }
                    else
                    {
                        if ((iMyGoalLocation().magnitude - oiLocation(Myself).magnitude) < 10)
                        {
                            next_state = StartGoingAway(iMyGoalLocation());
                        }
                    }
                }
                break;

            case ISSREventType.onCollision:
                next_state = ProcessCollision();
                break;

            default:
                //Debug.LogErrorFormat("{0}: evento {1} no considerado en estado {2}", Myself.Name,current_event, current_state);
                break;
        }

        return next_state;
    }

    ISSRState SF_WaitforNoStonesMoving() //Estado de espera porque ya hay alguna piedra moviendose
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onTickElapsed:
                if (iMovingStonesInMyTeam() == 0) //Si mi equipo no está moviendo ninguna piedra
                {
                    acGotoLocation(iMyGoalLocation()); //Voy a la meta con la piedra
                    if (acCheckError())
                        next_state = ISSRState.Error;
                    else
                    {
                        next_state = ISSRState.GoingToGoalWithSmallStone;
                        Debug.LogFormat("{0}: piedra {1} agarrada", Myself.Name, focus_object.Name);
                    }
                }
                break;
            case ISSREventType.onUngrip:
                SStoneIsAvailable(focus_object, true);
                next_state = GetSStone(focus_object);
                break;
            default:
                //Debug.LogErrorFormat("{0}: evento {1} no considerado en estado {2}", Myself.Name,current_event, current_state);
                break;
        }

        return next_state;
    }

    ISSRState SF_GoingToGripSmallStone() //Función para ir a coger una piedra pequeña
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onGripSuccess:
                focus_object.TimeStamp = Time.time;
                SStoneIsAvailable(focus_object, false);
                next_state = ISSRState.WaitforNoStonesMoving;
                break;
            case ISSREventType.onEnterSensingArea:
                if (object_just_seen.Equals(focus_object)) //Si la piedra que veo es la que busco, intento ir a por ella
                    next_state = GetSStone(focus_object);
                break;
            case ISSREventType.onCollision:
                if (colliding_object.Equals(focus_object))//Si el objeto con el que he colisionado es la piedra que quiero, la cojo
                    next_state = GetSStone(focus_object);
                else
                    next_state = ProcessCollision();
                break;
            case ISSREventType.onDestArrived:
            case ISSREventType.onGripFailure:
            case ISSREventType.onObjectLost:
                focus_object.TimeStamp = Time.time;
                SStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;
                break;
            case ISSREventType.onTickElapsed:
                    next_state = GetSStone(focus_object);
                break;
            case ISSREventType.onMsgArrived:
                if (user_msg_code == (int)EEVA_MsgCode.GetOutMyWay)
                {
                    last_state = current_state;
                    next_state = StartFlee(focus_location);
                }
                break;
            default:
                //Debug.LogErrorFormat("{0}: evento {1} no considerado en estado {2}", Myself.Name,current_event, current_state);
                break;
        }

        return next_state;
    }

    ISSRState SF_GoingToGoalWithSmallStone() //Estado de ir a meta con una piedra pequeña
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onGObjectScored:
                next_state = ISSRState.Idle;
                Debug.LogFormat("{0}: piedra {1} entregada en meta", Myself.Name, focus_object.Name);
                break;
            case ISSREventType.onCollision:
            case ISSREventType.onGObjectCollision:
                next_state = ProcessCollision();
                break;
            default:
                //Debug.LogErrorFormat("{0}: evento {1} no considerado en estado {2}", Myself.Name,current_event, current_state);
                break;
        }

        return next_state;
    }

    ISSRState SF_AvoidingObstacle() //Estado para evitar obtáculos
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onCollision:
            case ISSREventType.onGObjectCollision:
                next_state = ProcessCollision();
                break;

            case ISSREventType.onDestArrived:
                next_state = ResumeAfterCollision();
                break;

            case ISSREventType.onGObjectScored:
                next_state = ISSRState.Idle;
                break;

            case ISSREventType.onManyCollisions:
                acSetTimer(UnityEngine.Random.value * 2f);
                next_state = ISSRState.SleepingAfterCollisions;
                break;
            case ISSREventType.onMsgArrived:
                if (user_msg_code == (int)EEVA_MsgCode.EveryoneIsHere)
                {
                    acStop();
                    if (acCheckError())
                        next_state = ISSRState.Error;
                    else
                        next_state = ISSRState.Idle;
                }
                break;
        }

        return next_state;
    }

    ISSRState SF_SleepingAfterCollisions()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onTimerOut:
                next_state = ResumeAfterCollision();
                break;
            case ISSREventType.onUngrip:
                focus_object.TimeStamp = Time.time;
                SStoneIsAvailable(focus_object, true);
                next_state = GetSStone(focus_object);
                break;
            case ISSREventType.onMsgArrived:
                if (user_msg_code == (int)EEVA_MsgCode.EveryoneIsHere)
                {
                    acStop();
                    if (acCheckError())
                        next_state = ISSRState.Error;
                    else
                        next_state = ISSRState.Idle;
                }
                break;
        }

        return next_state;
    }

    ISSRState SF_GoingToGripBigStone()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onTickElapsed:
                next_state = GetBStone(focus_object);
                break;
            case ISSREventType.onEnterSensingArea:
                if (object_just_seen.Equals(focus_object)) //Si la piedra que veo es la que busco, intento ir a por ella
                    next_state = GetBStone(focus_object);
                break;
            case ISSREventType.onGripSuccess:
                if (oiGrippingAgents(focus_object) > 1)
                {
                    focus_object.TimeStamp = Time.time;
                    BStoneIsAvailable(focus_object, false);
                    next_state = ISSRState.WaitforNoStonesMovingBigStone;
                }
                else
                    next_state = ISSRState.WaitingForHelpToMoveBigStone;
                break;
            case ISSREventType.onCollision:
                if (colliding_object.Equals(focus_object))
                    next_state = GetBStone(focus_object);
                else
                    next_state = ProcessCollision();
                break;
            case ISSREventType.onGripFailure:
            case ISSREventType.onObjectLost:
            case ISSREventType.onDestArrived:
                focus_object.TimeStamp = Time.time;
                BStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;
                break;
            case ISSREventType.onMsgArrived:
                if (user_msg_code == (int)EEVA_MsgCode.GetOutMyWay)
                {
                    last_state = current_state;
                    next_state = StartFlee(focus_location);
                }
                break;
        }

        return next_state;
    }

    ISSRState SF_WaitingForHelpToMoveBigStone()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onAnotherAgentGripped:
                focus_object.TimeStamp = Time.time;
                BStoneIsAvailable(focus_object, false);
                next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;
            case ISSREventType.onUngrip:
                next_state = GetBStone(focus_object);
                break;
        }

        return next_state;
    }

    ISSRState SF_WaitforNoStonesMovingBigStone()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onAnotherAgentUngripped:
                focus_object.TimeStamp = Time.time;
                BStoneIsAvailable(focus_object, true);
                next_state = ISSRState.WaitingForHelpToMoveBigStone;
                break;
            case ISSREventType.onTickElapsed:
                if (iMovingStonesInMyTeam() == 0)
                {
                    acSendMsgObj(ISSRMsgCode.Assert, (int)EEVA_MsgCode.LetsGoToGoal, focus_object);
                    acGotoLocation(iMyGoalLocation());
                    if (acCheckError())
                        next_state = ISSRState.Error;
                    else
                        next_state = ISSRState.GoingToGoalWithBigStone;
                }
                break;
            case ISSREventType.onMsgArrived:
                //Esta comprobación ya la hacemos en ProccessMsg, hace falta hacerla aquí también??
                if (user_msg_code == (int)EEVA_MsgCode.LetsGoToGoal && msg_obj.Equals(focus_object) && iMovingStonesInMyTeam() == 0)
                {
                    acGotoLocation(iMyGoalLocation());
                    if (acCheckError())
                        next_state = ISSRState.Error;
                    else
                        next_state = ISSRState.GoingToGoalWithBigStone;
                }
                break;
            case ISSREventType.onUngrip:
                next_state = GetBStone(focus_object);
                focus_object.TimeStamp = Time.time;
                BStoneIsAvailable(focus_object, true);
                break;
        }

        return next_state;
    }

    ISSRState SF_GoingToGoalWithBigStone()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onGObjectScored:
                next_state = ISSRState.Idle;
                break;
            case ISSREventType.onPushTimeOut:
                next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;
                //Según lo que pone en la P/ nunca se entraría a onUngrip aquí -> siempre procesaria primero onCollision 
                //y depués procesaría el onUngrip de WaitforNoStonesMoving
            case ISSREventType.onCollision:
                //Mejora de la propuesta de la P7 -> si recibo onCollision y ya no tengo un objeto 
                //agarrado es porque he recibido yo el golpe (me evito usar también el evento onUngrip y procesar 
                //primero onCollision -> pasando al estado WaitforNoStonesMoving, y después por onUngrip)
                if (GrippedObject != null)
                    next_state = ISSRState.WaitforNoStonesMovingBigStone;
                else
                {
                    //Si la mejora da problemas -> a case onUngrip
                    if (oiSensable(focus_object))
                    {
                        focus_object.TimeStamp = Time.time;
                        BStoneIsAvailable(focus_object, true);
                        acGripObject(focus_object);
                        if (acCheckError())
                            next_state = ISSRState.Error;
                        else
                            //No seria mejor usar GetBStone (asi si ya habia dos agentes cogiendo la piedra por lo que sea pueden avanzar sin ti)
                            //next_state = ISSRState.GoingToGripBigStone;
                            next_state = GetBStone(focus_object);
                    }
                    else
                        next_state = ISSRState.Idle;
                }
                break;
            case ISSREventType.onGObjectCollision:
            case ISSREventType.onStop:
                next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;
            case ISSREventType.onAnotherAgentUngripped:
                //Si habia mas de dos agentes y uno de suelta puedo seguir moviendome
                if (oiGrippingAgents(GrippedObject) < 2)
                    next_state = ISSRState.WaitingForHelpToMoveBigStone;
                break;
            case ISSREventType.onTickElapsed:
                acSendMsg(ISSRMsgCode.Assert, (int)EEVA_MsgCode.GetOutMyWay, oiLocation(GrippedObject));
                break;
        }

        return next_state;
    }

    ISSRState SF_Scouting()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            //case ISSREventType.onDestArrived:
            case ISSREventType.onTickElapsed:
                focus_object = ISSRHelp.Get_next_available_stone_closer_to_me(this);

                //Si hay alguno (focus_object está definido)
                //No se puede ir directamente al estado GoingToGripBig/SmallStone con GetBStone o GetSStone??
                //Estamos pidiendo la piedra más cercana para luego en Idle hacer lo mismo
                if (focus_object != null)
                {
                    //next_state = ISSRState.Idle;
                    if (focus_object.type == ISSR_Type.SmallStone)
                    {
                        next_state = ISSRState.GoingToGripSmallStone;
                        focus_object.TimeStamp = Time.time;
                        acSendMsgObj(ISSRMsgCode.Assert, (int)EEVA_MsgCode.StoneSelected, focus_object, oiLocation(Myself) - oiLastLocation(focus_object));
                    }
                    else
                        next_state = ISSRState.GoingToGripBigStone;
                }
                else
                {
                    if (ISSRHelp.UpdateVisitedScoutingLocation(this))
                        next_state = next_exploration();
                    else
                    {
                        if (!Valid_Locations.Contains(focus_location))
                            next_state = next_exploration();
                    }
                }
                break;
            case ISSREventType.onCollision:
                next_state = ProcessCollision();
                break;
            //Si ya esta en el manejador aqui que hago???
            case ISSREventType.onMsgArrived:
                if (user_msg_code == (int)EEVA_MsgCode.GetOutMyWay)
                {
                    last_state = current_state;
                    next_state = StartFlee(focus_location);
                }
                break;
            //case ISSREventType.onStop:
                //next_state = ISSRState.Idle;
                //break;
            default:
                Debug.LogFormat("{0}: evento {1} en estado Scouting no considerado", Myself.Name, current_event);
                break;
        }

        return next_state;
    }

    ISSRState SF_GoingAway()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onCollision:
                if (!oiIsAWall(colliding_object))
                    next_state = ProcessCollision();
                break;
            case ISSREventType.onDestArrived:
                next_state = ISSRState.Idle;
                break;
        }

        return next_state;
    }

    ISSRState SF_GoingToMeetingPoint()
    {
        ISSRState next_state = current_state;
        switch (current_event)
        {
            case ISSREventType.onCollision:
                next_state = ProcessCollision();
                break;
            case ISSREventType.onTickElapsed:
                Vector3 distaciaAMeta = oiLocation(Myself) - iMyGoalLocation();
                if(distaciaAMeta.magnitude < iSensingRange() / 2)
                {
                    acStop();
                    if (acCheckError())
                        next_state = ISSRState.Error;
                    else
                        next_state = ISSRState.WaitingForPartners;
                }
                break;
            case ISSREventType.onMsgArrived:
                if (user_msg_code == (int)EEVA_MsgCode.EveryoneIsHere)
                {
                    acStop();
                    if (acCheckError())
                        next_state = ISSRState.Error;
                    else
                        next_state = ISSRState.Idle;
                }
                break;
        }

        return next_state;
    }

    ISSRState SF_WaitingForPartners()
    {
        ISSRState next_state = current_state;
        switch (current_event)
        {
            case ISSREventType.onTickElapsed:
                if (ISSRHelp.NumberOfObjectsOfTypeInList(SensableObjects, Myself.type) == iAgentsPerTeam() - 1)
                {
                    acSendMsg(ISSRMsgCode.Assert, (int)EEVA_MsgCode.EveryoneIsHere);
                    next_state = ISSRState.Idle;
                }
                break;
            case ISSREventType.onMsgArrived:
                if (user_msg_code == (int)EEVA_MsgCode.EveryoneIsHere)
                {
                    acStop();
                    if (acCheckError())
                        next_state = ISSRState.Error;
                    else
                        next_state = ISSRState.Idle;
                }
                break;
        }
        return next_state;
    }

    ISSRState SF_GettingOutOfTheWay()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onDestArrived:
                next_state = ResumeAfterCollision();
                break;
            case ISSREventType.onMsgArrived:
                if (user_msg_code == (int)EEVA_MsgCode.GetOutMyWay)
                {
                    next_state = StartFlee(focus_location);
                }
                break;
            case ISSREventType.onCollision:
                next_state = ProcessCollision();
                break;
        }

        return next_state;
    }

    ISSRState SF_White() //Estado de espera porque ya hay alguna piedra moviendose
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onTickElapsed:
                if (iMovingStonesInMyTeam() == 0) //Si mi equipo no está moviendo ninguna piedra
                {
                    safe_position(); //Directamente busco una posición segura (si no ya sé que me voy a chocar)
                    if (acCheckError())
                        next_state = ISSRState.Error;
                    else
                    {
                        next_state = ISSRState.GoingToGoalWithSmallStone;
                        Debug.LogFormat("{0}: piedra {1} agarrada", Myself.Name, focus_object.Name);
                    }
                }
                break;
            case ISSREventType.onUngrip:
                focus_object.TimeStamp = Time.time;
                SStoneIsAvailable(focus_object, true);
                next_state = GetSStone(focus_object);
                break;
            default:
                //Debug.LogErrorFormat("{0}: evento {1} no considerado en estado {2}", Myself.Name,current_event, current_state);
                break;
        }

        return next_state;
    }

    /*** ---------------------------------------------------------------
                          Funciones auxiliares 
    ---------------------------------------------------------------- ***/

    ISSRState ProcessCollision()  // Procesar colisión 
    {
        ISSRState next_state = current_state;

        switch (current_state)
        {
            case ISSRState.GoingToGripSmallStone:
                last_state = current_state;
                next_state = safe_position();
                break;

            case ISSRState.GoingToGripBigStone:
                last_state = current_state;
                next_state = safe_position();
                break;
            
            case ISSRState.GoingToGoalWithSmallStone:
                last_state = current_state;
                if (iMovingStonesInMyTeam() == 0)
                    next_state = safe_position();
                else
                {
                    //Aquí podría estar bien usar un estado White que compruebe si sigo teniendo un obstaculo delante
                    //y me permita calcular directamente una posición segura en vez de intentar ir directo a la meta
                    //Haciendo los cálculos me sale que la distancia aproximada de un objeto con el que chocado puede ser de 0.9
                    //podemos perder tiempo si esquivamos algo que no debemos esquivar y la distancia a una piedra grande y a una piedra pequeña es muy distinta
                    //Se pueden hacer medidas personalizadas: si es un agente voy a WaitforNoStonesMoving y si es piedra pequeña o grande mido la distancia aproximada 
                    //en función del radio del objeto (aunque quiza lo mas eficaz seria ver si la el objeto es piedra o no y en funcion de eso esquivar 
                    //la siguiente vez o ir directos a la meta)
                    if (colliding_object.type == ISSR_Type.BigStone || colliding_object.type == ISSR_Type.SmallStone)
                        next_state = ISSRState.White;
                    else
                        next_state = ISSRState.WaitforNoStonesMoving;
                }
                break;

            case ISSRState.AvoidingObstacle:
                if (iMovingStonesInMyTeam() > 0 && last_state == ISSRState.GoingToGoalWithSmallStone)
                    next_state = ISSRState.WaitforNoStonesMoving;
                else
                    //Aquí mismo tema que en el case de GoingToGoalWithSmallStone
                    next_state = safe_position();
                break;

            case ISSRState.Scouting:
                last_state = current_state;
                next_state = safe_position();
                break;

            case ISSRState.GoingToMeetingPoint:
                last_state = current_state;
                next_state = safe_position();
                break;

            case ISSRState.GettingOutOfTheWay:
                next_state = safe_position();
                break;

            case ISSRState.Idle:
                last_state = current_state;
                next_state = safe_position();
                break;

            default:
                Debug.LogErrorFormat("ProcessCollision() en {0}, estado {1} no considerado al colisionar", Myself.Name, current_state);
                break;
        }

        return next_state;
    }


    ISSRState ResumeAfterCollision() // Continuar con lo que se estaba haciendo en el momento de la colisión.
    {
        ISSRState next_state = current_state;

        switch (last_state)  // Según estado anterior 
        {
            case ISSRState.GoingToGripSmallStone:
                next_state = GetSStone(focus_object);  // Volver a pedir coger piedra o ir a su lugar
                break;

            case ISSRState.GoingToGoalWithSmallStone:
                if (iMovingStonesInMyTeam() == 0)
                {
                    acGotoLocation(iMyGoalLocation());  // volver a pedir ir a la meta
                    if (acCheckError())
                        next_state = ISSRState.Error;
                    else
                        next_state = ISSRState.GoingToGoalWithSmallStone;
                }
                else
                    next_state = ISSRState.WaitforNoStonesMoving;
                break;

            case ISSRState.GoingToGripBigStone:
                next_state = GetBStone(focus_object);
                break;

            case ISSRState.Scouting:
                acGotoLocation(focus_location);
                if (acCheckError())
                    next_state = ISSRState.Error;
                else
                    next_state = ISSRState.Scouting;
                break;

            case ISSRState.GoingToMeetingPoint:
                acGotoLocation(iMyGoalLocation());
                if (acCheckError())
                    next_state = ISSRState.Error;
                else
                    next_state = ISSRState.GoingToMeetingPoint;
                break;

            case ISSRState.Idle:
                next_state = ISSRState.Idle;
                break;

            default:
                Debug.LogErrorFormat("{0}, estado {1} no considerado al volver de colisi�n", Myself.Name, last_state);
                break;
        }
        return next_state;
    }

    ISSRState safe_position() //Calcular una posición segura después de una colisión
    {
        ISSRState next_state;
        Vector3 ps;

        if (oiIsAWall(colliding_object))
        {
            Vector3 nuevaDireccion;
            Vector3 myDirection = oiAgentDirection(Myself);

            if (colliding_object.type == ISSR_Type.NorthWall || colliding_object.type == ISSR_Type.SouthWall)
                nuevaDireccion = new Vector3(myDirection[0], myDirection[1], -myDirection[2]);
            else
                nuevaDireccion = new Vector3(-myDirection[0], myDirection[1], myDirection[2]);

            ps = oiLocation(Myself) + nuevaDireccion.normalized*2;
        }
        else
            ps = ISSRHelp.CalculateSafeLocation(this, colliding_object); //Calculo una ubicación segura

        acGotoLocation(ps); //Voy a esa ubicación
        if (acCheckError())
        {
            next_state = ISSRState.Error;
            Debug.LogFormat("{0}: Error al buscar una posición segura", Myself.Name);
        }
        else
        {
            next_state = ISSRState.AvoidingObstacle;
            Debug.LogFormat("{0}: Moviendose a una posición segura", Myself.Name);
        }

        return next_state;
    }

    void SStoneIsAvailable (ISSR_Object obj, bool available) //Función para marcar las piedras pequeñas como disponibles/no disponibles
    {
        ISSRHelp.UpdateStoneLists(obj, available, Valid_Small_Stones, Invalid_Small_Stones);
    }

    void BStoneIsAvailable(ISSR_Object obj, bool available) //Función para marcar las piedras pequeñas como disponibles/no disponibles
    {
        ISSRHelp.UpdateStoneLists(obj, available, Valid_Big_Stones, Invalid_Big_Stones);
    }

    private ISSRState GetSStone(ISSR_Object stone) //Función para evaluar si puedo ir a por una piedra e ir a por ella
    {
        ISSRState next_state = current_state;
        if (oiSensable(stone)) //Si puedo ver la piedra
        {
            if (oiGrippingAgents(stone) > 0) //Si hay un agente agarrándola
            {
                stone.TimeStamp = Time.time;
                SStoneIsAvailable(stone, false); //La marco como no disponible
                acStop(); //Me paro
                if (acCheckError())
                    next_state = ISSRState.Error;
                else
                    next_state = ISSRState.Idle;
            }
            else //Si ningún otro agente la tiene cogida
            {
                acGripObject(stone); //Voy a cogerla             
                if (acCheckError())
                    next_state = ISSRState.Error;
                else
                {
                    next_state = ISSRState.GoingToGripSmallStone;
                    acSendMsgObj(ISSRMsgCode.Assert, (int)EEVA_MsgCode.StoneSelected, stone, oiLocation(Myself) - oiLastLocation(stone));
                }
            }
        }
        else //Si la piedra no está a la vista
        {
            if (Vector3.Distance(oiLastLocation(stone), oiLocation(Myself)) > iSensingRange()) //Si aún no puedo ver al piedra
            {
                acGotoLocation(oiLastLocation(stone)); //Sigo yendo a dónde la ví por última vez
                next_state = ISSRState.GoingToGripSmallStone;
                acSendMsgObj(ISSRMsgCode.Assert, (int)EEVA_MsgCode.StoneSelected, stone, oiLocation(Myself) - oiLastLocation(stone));
                if (acCheckError())
                    next_state = ISSRState.Error;
            }
            else //Si debería ver la piedra, pero no la veo (no está)
            {
                stone.TimeStamp = Time.time;
                SStoneIsAvailable(stone, false); //Marco la piedra como no disponible
                acStop(); //Me paro
                if (acCheckError())
                    next_state = ISSRState.Error;
                else
                    next_state = ISSRState.Idle;
            }
        }
        return next_state;
    }

    private ISSRState GetBStone(ISSR_Object stone) //Función para evaluar si puedo ir a por una piedra e ir a por ella
    {
        ISSRState next_state = current_state;

        if (oiSensable(stone)) //Si puedo ver la piedra
        {
            //REVISAR!!!!!!!!
            if (oiGrippingAgents(stone) > 1 && GrippingAgentsBStoneMyTeam(stone) > oiGrippingAgents(stone) - GrippingAgentsBStoneMyTeam(stone))
            {
                stone.TimeStamp = Time.time;
                SStoneIsAvailable(stone, false); //La marco como no disponible
                acStop(); //Me paro
                if (acCheckError())
                    next_state = ISSRState.Error;
                else
                    next_state = ISSRState.Idle;
            }
            else //Si ningún otro agente la tiene cogida
            {
                acGripObject(stone); //Voy a cogerla
                if (acCheckError())
                    next_state = ISSRState.Error;
                else
                    next_state = ISSRState.GoingToGripBigStone;
            }
        }
        else //Si la piedra no está a la vista
        {
            if (Vector3.Distance(oiLastLocation(stone), oiLocation(Myself)) > iSensingRange()) //Si aún no puedo ver al piedra
            {
                acGotoLocation(oiLastLocation(stone)); //Sigo yendo a dónde la ví por última vez
                if (acCheckError())
                    next_state = ISSRState.Error;
                else
                    next_state = ISSRState.GoingToGripBigStone;
            }
            else //Si debería ver la piedra, pero no la veo (no está)
            {
                stone.TimeStamp = Time.time;
                SStoneIsAvailable(stone, false); //Marco la piedra como no disponible
                acStop(); //Me paro
                if (acCheckError())
                    next_state = ISSRState.Error;
                else
                    next_state = ISSRState.Idle;
            }
        }

        return next_state;
    }

    void Share()
    {
        foreach (ISSR_Object obj in Valid_Small_Stones)
        {
            acSendMsgObj(ISSRMsgCode.Assert, (int)EEVA_MsgCode.AvailableStone, obj);
            acSendMsgObj(ISSRMsgCode.Query, (int)EEVA_MsgCode.NonAvailableStone, obj);
        }

        foreach (ISSR_Object obj in Invalid_Small_Stones)
        {
            acSendMsgObj(ISSRMsgCode.Assert, (int)EEVA_MsgCode.NonAvailableStone, obj);
            acSendMsgObj(ISSRMsgCode.Query, (int)EEVA_MsgCode.AvailableStone, obj);
        }     

        foreach (ISSR_Object obj in Valid_Big_Stones)
        {
            acSendMsgObj(ISSRMsgCode.Assert, (int)EEVA_MsgCode.AvailableStone, obj);
            acSendMsgObj(ISSRMsgCode.Query, (int)EEVA_MsgCode.NonAvailableStone, obj);
        }

        foreach (ISSR_Object obj in Invalid_Big_Stones)
        {
            acSendMsgObj(ISSRMsgCode.Assert, (int)EEVA_MsgCode.NonAvailableStone, obj);
            acSendMsgObj(ISSRMsgCode.Query, (int)EEVA_MsgCode.AvailableStone, obj);
        }   

        foreach (Vector3 loc in Invalid_Locations)
            acSendMsg(ISSRMsgCode.Assert, (int)EEVA_MsgCode.ExploredLocation, loc);

        foreach (Vector3 loc in Valid_Locations)
            acSendMsg(ISSRMsgCode.Query, (int)EEVA_MsgCode.ExploredLocation, loc);
    }

    void ProcessMessage(ISSR_Message msg)
    {
        if(msg.code == ISSRMsgCode.Assert && oiIsAgentInMyTeam(msg.Sender))
        {
            if (msg.usercode == (int)EEVA_MsgCode.AvailableStone || msg.usercode == (int)EEVA_MsgCode.NonAvailableStone)
            {
                bool available = false;
                ISSR_Object obj = msg_obj; //Por si el valor de msg_obj cambia en mitad (no se si puede ocurrir o no)

                if (msg.usercode == (int)EEVA_MsgCode.AvailableStone)
                    available = true;

                //msg_obj contiene siempre el mismo objeto que msg.Obj??
                if (msg_obj.type == ISSR_Type.SmallStone)
                    SStoneIsAvailable(obj, available);

                if (msg_obj.type == ISSR_Type.BigStone)
                    BStoneIsAvailable(obj, available);

                if (Stones.Contains(msg_obj) && focus_object != null)
                {
                    int index = Stones.IndexOf(msg_obj);
                    if(msg_obj.TimeStamp > Stones[index].TimeStamp)
                        Stones.Remove(msg_obj);
                }
            }
            // Que para si yo voy a por esa piedra pero no la tengo agarrada y me llega este mensaje?? 
            //(no sería mejor compararlo con GrippedObject)???
            else if (msg.usercode == (int)EEVA_MsgCode.LetsGoToGoal && GrippedObject != null)
            {
                if (msg_obj.Equals(GrippedObject))
                    current_state = AgentStateMachine();
            }
            else if (msg.usercode == (int)EEVA_MsgCode.ExploredLocation)
            {
                Vector3 loc = msg_location; //Por si el valor de msg_location cambia en mitad (no se si puede ocurrir o no)
                if (Valid_Locations.Contains(loc))
                {
                    Invalid_Locations.Add(loc);
                    Valid_Locations.Remove(loc);
                }
            }
            else if (user_msg_code == (int)EEVA_MsgCode.StoneSelected && focus_object != null)
            {
                if (msg_obj.Equals(focus_object))
                {
                    Vector3 distance = oiLocation(Myself) - oiLastLocation(focus_object);
                    if (msg.fvalue < distance.magnitude)
                    {
                        if (Valid_Small_Stones.Contains(focus_object))
                            Valid_Small_Stones.Remove(focus_object);
                        focus_object.TimeStamp = Time.time;
                        Stones.Add(focus_object);
                    }
                }
            }
            else if (user_msg_code == (int)EEVA_MsgCode.EveryoneIsHere)
                current_state = AgentStateMachine();
            else if (user_msg_code == (int)EEVA_MsgCode.GetOutMyWay)
            {
                focus_location = msg.location;
                current_state = AgentStateMachine();
            }
        }
    }

    ISSRState StartGoingAway(Vector3 away_from)
    {
        ISSRState next_state;

        Vector3 direction = oiLocation(Myself) - away_from;
        acGotoLocation(oiLocation(Myself) + direction.normalized * 20);
        if (acCheckError())
            next_state = ISSRState.Error;
        else
            next_state = ISSRState.GoingAway;

        return next_state;
    }

    ISSRState next_exploration()
    {
        ISSRState next_state = current_state;
        int remain;
        Vector3 punto_expl = ISSRHelp.GetCloserToMeLocationInList(this, Valid_Locations, out remain);
        if (remain > 0)
        {
            acGotoLocation(punto_expl);
            if (acCheckError())
                next_state = ISSRState.Error;
        }
        else
            next_state = ISSRState.Idle;

        return next_state;
    }

    int GrippingAgentsBStoneMyTeam (ISSR_Object stone)
    {
        int nobj = 0;

        if (oiSensable(stone))
        {
            foreach (ISSR_Object obj in SensableObjects)
            {
                if (obj.type == Myself.type && stone.Equals(oiAgentGrippedObject(obj)))
                    nobj++;
            }
            if (stone.Equals(GrippedObject))
                nobj++;
        }
        else
            nobj = -1;

        return nobj;
    }

    ISSRState StartFlee(Vector3 flee_from)
    {
        ISSRState next_state = current_state;
        Vector3 direction = oiLocation(Myself) - flee_from;
        if (direction.magnitude < 3)
        {
            Vector3 move_to = oiLocation(Myself) + direction.normalized * 5f;
            acGotoLocation(move_to);
            if (acCheckError())
                next_state = ISSRState.Error;
            else
                next_state = ISSRState.GettingOutOfTheWay;
        }  
        else if (direction.magnitude < 5)
        {
            direction = ISSRHelp.AwayFromPathDirection(iMyGoalLocation(), flee_from, oiLocation(Myself));
            Vector3 move_to = oiLocation(Myself) + direction.normalized * 5f;
            acGotoLocation(move_to);
            if (acCheckError())
                next_state = ISSRState.Error;
            else
                next_state = ISSRState.GettingOutOfTheWay;
        }

        return next_state;
    }
}