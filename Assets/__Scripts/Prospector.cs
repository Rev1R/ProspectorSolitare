﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;  //Будет использоваться позже
using UnityEngine.UI;  //Будет ипользоваться позже

public class Prospector : MonoBehaviour
{
    static public Prospector S;

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public float xOffset = 3;
    public float yOffset = -2.5f;
    public Vector3 layoutCenter;
    public Vector2 fsPosMid = new Vector2(0.5f, 0.90f);
    public Vector2 fsPosRun = new Vector2(0.5f, 0.75f);
    public Vector2 fsPosMid2 = new Vector2(0.4f, 1.0f);
    public Vector2 fsPosEnd = new Vector2(0.5f, 0.95f);
    public float reloadDelay = 2f;   //задержка между раундами 2 секунды
    public Text gameOverText, roundResultText, highScoreText;

    [Header("Set Dynamically")]
    public Deck deck;
    public Layout layout;
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;
    public FloatingScore fsRun;

    void Awake()
    {
        S = this; // Подготовка обьекта-одиночки Prospector
        SetUpUITexts();
        
    }
    void SetUpUITexts()
    {
        //настроить обьект HighScore
        GameObject go = GameObject.Find("HighScore");
        if(go != null)
        {
            highScoreText = go.GetComponent<Text>();
        }
        int highScore = ScoreManager.HIGH_SCORE;
        string hScore = "High Score: " + Utils.AddCommasToNumber(highScore);
        go.GetComponent<Text>().text = hScore;

        //настроить надписи, отображаемые в конце раунда
        go = GameObject.Find("GameOver");
        if(go != null)
        {
            gameOverText = go.GetComponent<Text>();
        }
        go = GameObject.Find("RoundResult");
        if(go != null)
        {
            roundResultText = go.GetComponent<Text>();
        }
        //скрыть надписи 
        ShowResultsUI(false);
    }
    void ShowResultsUI(bool show)
    {
        gameOverText.gameObject.SetActive(show);
        roundResultText.gameObject.SetActive(show);
    }
    void Start()
    {

        Scoreboard.S.score = ScoreManager.SCORE;

        deck = GetComponent<Deck>();  //получить компонент Deck
        deck.InitDeck(deckXML.text);  //передать еиу DeckXML
        Deck.Shuffle(ref deck.cards); //перемешать колоду, передав ее по ссылке  //a 

        //этот фрагмент нужно закомментировать; сейчас мы создаем фактическую расскладку
        // Card c;
        // for(int cNum=0; cNum<deck.cards.Count; cNum++)
        // {
        //    c = deck.cards[cNum];
        //    c.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);
        // }

        layout = GetComponent<Layout>();  //получить компонент Layout
        layout.ReadLayout(layoutXML.text); //передать ему содержимое LayoutXML
        drawPile = ConvertListCardsToListCardProspectors(deck.cards);
        LayoutGame();
    }
    List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD)
    {
        List<CardProspector> lCP = new List<CardProspector>();
        CardProspector tCP;
        foreach(Card tCD in lCD)
        {
            tCP = tCD as CardProspector;         //a
            lCP.Add(tCP);
        }
        return (lCP);
    }
    //Функция Draw снимает одну карту с вершины drawPile и возвращает ее
    CardProspector Draw()
    {
        CardProspector cd = drawPile[0];  //Снять 0-ю карту CardProspector
        drawPile.RemoveAt(0);             //Удалить из <List> drawPile
        return (cd);                      //И вернуть ее
    }
    // LayoutGame() размещает карты в начальной раскладке - "шахте"
    void LayoutGame()        //a
    {
        //создать пустой игровой обьект, который будет служить центром раскладки
        if(layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            // ^ Создать пустой игровой обьект с именем _LayoutAnchor в иерархии
            layoutAnchor = tGO.transform;  //Получить его компонент Transform
            layoutAnchor.transform.position = layoutCenter;  //поместить в центр
        }
        CardProspector cp;
        //разложить карты
        foreach(SlotDef tSD in layout.slotDefs)
        {        // ^ выполнить обход всех определений SlotDef в layout.slotDefs
            cp = Draw();  //выбрать первую карту (сверху) из стопки drawPile
            cp.faceUp = tSD.faceUp;  //установить ее признак faceUp в соответствии с определением в SlotDef
            cp.transform.parent = layoutAnchor;  //Назначить layoutAnchor ее родителем

            //Эта операция заменит предыдущего родителя: deck.deckAnhor,
            //который после запуска игры отображается в иерархии с именем _Deck.
            cp.transform.localPosition = new Vector3(layout.multiplier.x * tSD.x, layout.multiplier.y * tSD.y, -tSD.layerID);
            //^ установить localPosition карты в соответствии с определением в SlotDef
            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            //Карты CardProspector в основной раскладке имеют состояние CardState.tableau
            cp.state = eCardState.tableau;
            cp.SetSortingLayerName(tSD.layerName);  //назначить слой сортировки

            tableau.Add(cp);  //добавить карту в список tableau

        }
        //настроить списки карт, мешающих перевернуть данную
        foreach(CardProspector tCP in tableau)
        {
            foreach(int hid in tCP.slotDef.heddenBy)
            {
                cp = FindCardByLayoutID(hid);
                tCP.hiddenBy.Add(cp);
            }
        }
        //выбрать начальную целевую карту
        MoveToTarget(Draw( ));

        //разложить стопку свободных карт
        UpdateDrawPile();
    }
    //Преобразует номер слота LayoutID в экземпляр CardProspector с этим номером
    CardProspector FindCardByLayoutID(int layoutID)
    {
        foreach(CardProspector tCP in tableau)
        {
            //Поиск по всем картам в списке tableau
            if(tCP.layoutID == layoutID)
            {
                //если номер слота карты совпадает с исковым, вернуть ее
                return (tCP);
            }
        }
        //если ничего не найдено вернуть null
        return (null);
    }

    //поворачивает карты в основной раскладке лицевой стороной вверх или вниз
    void SetTableauFaces()
    {
        foreach(CardProspector cd in tableau)
        {
            bool faceUp = true;  //предположить, что карта должна быть повернута лицевой стороной вверх
            foreach(CardProspector  cover in cd.hiddenBy)
            {
                //если любая из карт, перекрывающую текущую присутствует в основной раскладке
                if(cover.state == eCardState.tableau)
                {
                    faceUp = false; //повернуть лицевой стороной вниз
                }
            }
            cd.faceUp = faceUp;  //повернуть карту так или иначе
        }
    }
        

    //Перемещает текущую целевую карту в стопку сброшенных карт
    void MoveToDiscard(CardProspector cd)
    {
        //Установить состояние карты как discard (сброшена)
        cd.state = eCardState.descard;
        discardPile.Add(cd); //добавить ее в список discardPile
        cd.transform.parent = layoutAnchor;  //обновить значение transform.parent

        //переместить эту карту в позицию стопки сброшенных карт
        cd.transform.localPosition = new Vector3(
            layout.multiplier.x * layout.discardPile.x,
            layout.multiplier.y * layout.discardPile.y,
            -layout.discardPile.layerID + 0.5f);
        cd.faceUp = true;
        //поместить поверх стопки для сортировки по глубине
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }
    //делает карту cd новой целевой картой
    void MoveToTarget(CardProspector cd)
    {
        //если целевая карта существует, поместить ее в стопку сброшенных карт
        if (target != null) MoveToDiscard(target);
        target = cd;  // cd - новая целевая карта
        cd.state = eCardState.target;
        cd.transform.parent = layoutAnchor;

        //переместить на место целевой карты
        cd.transform.localPosition = new Vector3(
            layout.multiplier.x * layout.discardPile.x,
            layout.multiplier.y * layout.discardPile.y,
            -layout.discardPile.layerID);
        cd.faceUp = true; //повернуть лицевой стороной вверх
        //настроить сортировку по глубине
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);
    }

    //Раскладывает стопку свободных карт, чтобы было видно, сколько карт осталось
    void UpdateDrawPile()
    {
        CardProspector cd;
        //Выполнить обход всех карт в drawPile
        for (int i = 0; i < drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;

            //расположить с учетом смещения layout.drawPile.stagger
            Vector2 dpStagger = layout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(
                layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
                layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y)
                - layout.drawPile.layerID + 0.1f * i);
            cd.faceUp = false;  //повернуть лицевой стороной вниз
            cd.state = eCardState.drawpile;
            //настроить сортировку по глубине
            cd.SetSortingLayerName(layout.drawPile.layerName);
            cd.SetSortOrder(-10 * i);
        }
    }

    //CardClicked вызывается в ответ на щелчок на любой карте
    public void CardClicked(CardProspector cd)
    {
        //реакция определяется состоянием карты
        switch (cd.state) 
        {
            case eCardState.target:
                //щелчок на целевой карте игнорируется
                break;
            case eCardState.drawpile:
                //щелчок на любой карте в стопке свободных карт приводит к смене целевой карты
                MoveToDiscard(target);  //переместить карту в discardPile
                MoveToTarget(Draw());   //переместить верхнюю свободную карту на место целевой 
                UpdateDrawPile();       //повторно разложить стопку свободных карт
                ScoreManager.EVENT(eScoreEvent.draw);
                FloatingScoreHandler(eScoreEvent.draw);
                break;
            case eCardState.tableau:
                //для карты в основной раскладке проверяется возможность ее перемещения на место целевой
                bool validMatch = true;
                if (!cd.faceUp)
                {
                    //карта, повернутая лицевой стороной вниз, не может перемещаться
                    validMatch = false;
                }
                if (!AdjacentRank(cd, target))
                {
                    //если правила старшинства не соблюдаются карта не может перемещаться
                    validMatch = false;
                    print("нельзя переместить карту");
                }
                if (!validMatch) return;  //выйти, если карта не может перемещаться

                //мы оказались здесь: Ура! карту можно переместить.
                tableau.Remove(cd);  //удалить из списка tableau
                MoveToTarget(cd);  //сделать эту карту целевой
                SetTableauFaces(); //повернуть карты в основной раскладке лицевой стороной вниз или вверх
                ScoreManager.EVENT(eScoreEvent.mine);
                FloatingScoreHandler(eScoreEvent.mine);
                break;
        }
        //проверить завершение игры
        CheckForGameOver();
    }

    //проверяет завершение игры
    void CheckForGameOver()
    {
        //если основная раскладка опустела, игра завершена
        if (tableau.Count == 0)
        {
            //Вызвать GameOver() с признаком победы
            GameOver(true);
            return;
        }

        //если есть еще свободные карты, игра не завершилась 
        if (drawPile.Count > 0)
        {
            return;
        }

        //проверить наличие допустимых ходов
        foreach(CardProspector cd in tableau)
        {
            if(AdjacentRank(cd,target))
            {
                //если есть допустимый ход, игра не завершилась
                return;
            }
        }
        //так как допустимых ходов нет, игра завершилась
        //Вызвать GameOver с признаком проигрыша
        GameOver(false);
    }

    //вызывается когда игра завершилась. 
    void GameOver(bool won)
    {
        int score = ScoreManager.SCORE;
        if (fsRun != null) score += fsRun.score;
        if (won)
        {
            gameOverText.text = "Round Over";
            roundResultText.text = "You won this round!\nRound Score: " + score;
            ShowResultsUI(true);
            //print("GameOver. You won!");   //комент строки
            ScoreManager.EVENT(eScoreEvent.gameWin);
            FloatingScoreHandler(eScoreEvent.gameWin);
        }
        else
        {
            gameOverText.text = "Game Over";
            if(ScoreManager.HIGH_SCORE <= score)
            {
                string str = "You got the high score!\nHigh Score: " + score;
                roundResultText.text = str;
            }
            else
            {
                roundResultText.text = "Your final score was: " + score;
            }
            ShowResultsUI(true);
            // print("GameOver. You lost..");   //комент строки
            ScoreManager.EVENT(eScoreEvent.gameLoss);
            FloatingScoreHandler(eScoreEvent.gameLoss);
        }
        //перезагрузить сцену и сбросить игру в исходное состояние
        //SceneManager.LoadScene("__Prospector_Scene_0");
        Invoke("ReloadLevel", reloadDelay);
    }
    void ReloadLevel()
    {
        //перезагрузить сцену и сбросить игру в исходное состояние
        SceneManager.LoadScene("__Prospector_Scene_0");
    }

    //возвращает true, если две карты соответствуют правилу старшинства
    //(с учетом циклического переноса старшинства между тузом и королем)
    public bool AdjacentRank(CardProspector c0, CardProspector c1)
    {
        //если любая из карт повернутая лицевой стороной вниз, правила старшинства не соблюдаются.
        if (!c0.faceUp || !c1.faceUp) return (false);

        //если достоинства карт отличаются на 1, правила старшинства соблюдается
        if(Mathf.Abs(c0.rank - c1.rank) == 1)
        {
            return (true);
        }
        //если одна карта - туз, а другая король, правила старшинства соблюдается
        if (c0.rank == 1 && c1.rank == 13) return (true);
        if (c0.rank == 13 && c1.rank == 1) return (true);

        //иначе вернуть false
        return (false);
    }

    //обрабатывает движение FloatingScore
    void FloatingScoreHandler(eScoreEvent evt)
    {
        List<Vector2> fsPts;
        switch (evt)
        {
            //в случае победы, проигрыша и щавершения хода выполняются одни и те же действия
            case eScoreEvent.draw:     //выбор свободной карты
            case eScoreEvent.gameWin:  //победа в раунде 
            case eScoreEvent.gameLoss: //проигрыш в раунде
                //добавить fsRun в Scoreboard
                if (fsRun != null)
                {
                    //создать точки для кривой безье
                    fsPts = new List<Vector2>();
                    fsPts.Add(fsPosRun);
                    fsPts.Add(fsPosMid2);
                    fsPts.Add(fsPosEnd);
                    fsRun.reportFinishTo = Scoreboard.S.gameObject;
                    fsRun.Init(fsPts, 0, 1);
                    //также скорректировать fontSize
                    fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
                    fsRun = null;  //очистить fsRun, чтобы создать заново
                }
                break;

            case eScoreEvent.mine: //удаление карты из основной раскладки
                                   //Создать FloatingScore для отображения этого количества очков
                FloatingScore fs;
                //Переместить из позиции указателя мыши mousePosition в fsPosRun 
                Vector2 p0 = Input.mousePosition;
                p0.x /= Screen.width;
                p0.y /= Screen.height;
                fsPts = new List<Vector2>();
                fsPts.Add(p0);
                fsPts.Add(fsPosMid);
                fsPts.Add(fsPosRun);
                fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
                fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
                if(fsRun == null)
                {
                    fsRun = fs;
                    fsRun.reportFinishTo = null;
                }
                else
                {
                    fs.reportFinishTo = fsRun.gameObject;
                }
                break;
        }
    }
}
