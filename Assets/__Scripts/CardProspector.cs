using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Перечисление, определяющее тип переменной, которая может принимать несколько прдопределенных значений

    public enum eCardState { drawpile, tableau, target, descard}   //a

public class CardProspector: Card      //CardProspector должен расширять Card
{
    [Header("Set Dynamically : CardProspector")]
    //так используется перечисление eCardState
    public eCardState state = eCardState.drawpile;

    //hiddenBy - список карт, не позволяющих перевернуть эту лицом вверх
    public List<CardProspector> hiddenBy = new List<CardProspector>();

    //layoutID определяет для этой карты ряд в раскладке
    public int layoutID;

    //класс SlotDef хранит информацию из элемента <slot> в LauoutXML
    public SlotDef slotDef;

    //определяет реакцию карт на щелчок мыши
    public override void OnMouseUpAsButton()
    {
        //вызвать мето CardClicker обьекта-одиночки Prospector
        Prospector.S.CardClicked(this);
        //а также версию этого метода в базовом классе(Card.cs)
        base.OnMouseUpAsButton();   //a
    }    
}