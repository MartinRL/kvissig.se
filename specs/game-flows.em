# Mer eller Mindre - Game Flows

## Game Rules

Mer eller Mindre är ett quizspel där spelarna gissar:
1. **Riktning**: Är A mer eller mindre än B?
2. **Differens**: Hur stor är skillnaden? (normaliserad 0-100)

### Poängsättning per fråga
- Differenspoäng = |gissad_diff - faktisk_diff| (alltid 0-100)
- Rätt riktning = -10 bonus
- Fel riktning = ingen bonus

### Vinnare
Lägsta totala poäng efter alla rundor vinner.
Negativa poäng är möjliga (rätt riktning + exakt diff = -10).

---

## Game Lifecycle

/CreateGame/
  %Host:CreateGameForm%
  [CreateGame]
    hostName
    numberOfRounds
  <Game:GameCreated>
    gameId
    hostPlayerId
    joinCode
    numberOfRounds
    createdAt
  {Lobby}
    gameId
    joinCode
    players
---
/JoinGame/
  %Player:JoinGameForm%
  [JoinGame]
    joinCode
    playerName
  <Game:PlayerJoined>
    gameId
    playerId
    playerName
    joinedAt
  {Lobby}
---
/StartGame/
  %Host:Lobby%
  [StartGame]
    gameId
  <Game:GameStarted>
    gameId
    startedAt
  <Game:QuestionPresented>
    gameId
    questionIndex
    questionText
    optionA
    optionB
  {Question}
---

## Question Round

/SubmitGuess/
  %Player:Question%
  [SubmitGuess]
    gameId
    playerId
    direction (mer|mindre)
    difference (0-100)
  <Game:GuessSubmitted>
    gameId
    playerId
    questionIndex
    direction
    difference
    submittedAt
  {WaitingForOthers}
---
/AllGuessesIn/
  <Game:GuessSubmitted>
  <Game:AllGuessesSubmitted>
    gameId
    questionIndex
  <Game:QuestionScored>
    gameId
    questionIndex
    correctDirection
    correctDifference
    playerScores[]
      playerId
      guessedDirection
      guessedDifference
      directionCorrect
      differencePoints
      bonusPoints
      roundScore
      totalScore
  {Results}
---

## Game Progression

/NextQuestion/
  %Host:Results%
  [NextQuestion]
    gameId
  <Game:QuestionPresented>
    gameId
    questionIndex
    questionText
    optionA
    optionB
  {Question}
---
/GameOver/
  <Game:QuestionScored>
  <Game:GameEnded>
    gameId
    finalScoreboard[]
      playerId
      playerName
      totalScore
      rank
    winnerId
    endedAt
  {FinalResults}
---

## Exception Flows

/JoinGame/
  [JoinGame]
    joinCode
    playerName
  !GameNotFound!
    joinCode
---
/JoinGame/
  [JoinGame]
    joinCode
    playerName
  !GameAlreadyStarted!
    gameId
---
/SubmitGuess/
  [SubmitGuess]
    gameId
    playerId
    direction
    difference
  !DifferenceOutOfRange!
    difference
    min:0
    max:100
---
/SubmitGuess/
  [SubmitGuess]
    gameId
    playerId
    direction
    difference
  !AlreadyGuessed!
    playerId
    questionIndex
---
/SubmitGuess/
  [SubmitGuess]
    gameId
    playerId
    direction
    difference
  !InvalidDirection!
    direction
---

## GWT Tests

### Lobby Tests

?GameCanBeCreated?
  [CreateGame]
    hostName:Martin
    numberOfRounds:10
  <Game:GameCreated>
    hostPlayerId:player-1
    numberOfRounds:10
---
?PlayerCanJoinLobby?
  <Game:GameCreated>
    gameId:game-1
    joinCode:ABC123
  [JoinGame]
    joinCode:ABC123
    playerName:Anna
  <Game:PlayerJoined>
    gameId:game-1
    playerName:Anna
---
?CannotJoinNonexistentGame?
  [JoinGame]
    joinCode:INVALID
    playerName:Erik
  !GameNotFound!
    joinCode:INVALID
---
?CannotJoinStartedGame?
  <Game:GameCreated>
    gameId:game-1
    joinCode:ABC123
  <Game:GameStarted>
    gameId:game-1
  [JoinGame]
    joinCode:ABC123
    playerName:Erik
  !GameAlreadyStarted!
    gameId:game-1
---

### Guess Tests

?GuessSubmittedSuccessfully?
  <Game:GameCreated>
    gameId:game-1
  <Game:PlayerJoined>
    gameId:game-1
    playerId:player-1
  <Game:GameStarted>
    gameId:game-1
  <Game:QuestionPresented>
    gameId:game-1
    questionIndex:0
  [SubmitGuess]
    gameId:game-1
    playerId:player-1
    direction:mer
    difference:42
  <Game:GuessSubmitted>
    gameId:game-1
    playerId:player-1
    direction:mer
    difference:42
---
?DifferenceOutOfRangeRejected?
  <Game:GameCreated>
    gameId:game-1
  <Game:GameStarted>
    gameId:game-1
  <Game:QuestionPresented>
    gameId:game-1
    questionIndex:0
  [SubmitGuess]
    gameId:game-1
    playerId:player-1
    direction:mer
    difference:150
  !DifferenceOutOfRange!
    difference:150
    min:0
    max:100
---
?InvalidDirectionRejected?
  <Game:GameCreated>
    gameId:game-1
  <Game:GameStarted>
    gameId:game-1
  <Game:QuestionPresented>
    gameId:game-1
    questionIndex:0
  [SubmitGuess]
    gameId:game-1
    playerId:player-1
    direction:maybe
    difference:50
  !InvalidDirection!
    direction:maybe
---
?CannotGuessAgainOnSameQuestion?
  <Game:GameCreated>
    gameId:game-1
  <Game:GameStarted>
    gameId:game-1
  <Game:QuestionPresented>
    gameId:game-1
    questionIndex:0
  <Game:GuessSubmitted>
    gameId:game-1
    playerId:player-1
    questionIndex:0
  [SubmitGuess]
    gameId:game-1
    playerId:player-1
    direction:mer
    difference:50
  !AlreadyGuessed!
    playerId:player-1
    questionIndex:0
---

### Scoring Tests

?CorrectDirectionGivesBonus?
  <Game:GameCreated>
    gameId:game-1
  <Game:PlayerJoined>
    playerId:player-1
  <Game:GameStarted>
    gameId:game-1
  <Game:QuestionPresented>
    gameId:game-1
    questionIndex:0
    correctDirection:mer
    correctDifference:35
  <Game:GuessSubmitted>
    playerId:player-1
    direction:mer
    difference:40
  <Game:AllGuessesSubmitted>
    gameId:game-1
  <Game:QuestionScored>
    playerScores[0]:
      playerId:player-1
      directionCorrect:true
      differencePoints:5
      bonusPoints:-10
      roundScore:-5
---
?WrongDirectionNoBonus?
  <Game:GameCreated>
    gameId:game-1
  <Game:PlayerJoined>
    playerId:player-1
  <Game:GameStarted>
    gameId:game-1
  <Game:QuestionPresented>
    gameId:game-1
    questionIndex:0
    correctDirection:mer
    correctDifference:35
  <Game:GuessSubmitted>
    playerId:player-1
    direction:mindre
    difference:40
  <Game:AllGuessesSubmitted>
    gameId:game-1
  <Game:QuestionScored>
    playerScores[0]:
      playerId:player-1
      directionCorrect:false
      differencePoints:5
      bonusPoints:0
      roundScore:5
---
?ExactDifferenceWithCorrectDirection?
  <Game:GameCreated>
    gameId:game-1
  <Game:PlayerJoined>
    playerId:player-1
  <Game:GameStarted>
    gameId:game-1
  <Game:QuestionPresented>
    gameId:game-1
    questionIndex:0
    correctDirection:mer
    correctDifference:50
  <Game:GuessSubmitted>
    playerId:player-1
    direction:mer
    difference:50
  <Game:AllGuessesSubmitted>
    gameId:game-1
  <Game:QuestionScored>
    playerScores[0]:
      playerId:player-1
      directionCorrect:true
      differencePoints:0
      bonusPoints:-10
      roundScore:-10
---
?ScoresAccumulateAcrossRounds?
  <Game:GameCreated>
    gameId:game-1
    numberOfRounds:2
  <Game:PlayerJoined>
    playerId:player-1
  <Game:GameStarted>
    gameId:game-1
  # Round 1: score -5
  <Game:QuestionScored>
    questionIndex:0
    playerScores[0]:
      playerId:player-1
      roundScore:-5
      totalScore:-5
  # Round 2: score 10
  <Game:QuestionScored>
    questionIndex:1
    playerScores[0]:
      playerId:player-1
      roundScore:10
      totalScore:5
---
?LowestScoreWins?
  <Game:GameCreated>
    gameId:game-1
    numberOfRounds:1
  <Game:PlayerJoined>
    playerId:player-1
    playerName:Anna
  <Game:PlayerJoined>
    playerId:player-2
    playerName:Erik
  <Game:GameStarted>
    gameId:game-1
  <Game:QuestionScored>
    questionIndex:0
    playerScores[0]:
      playerId:player-1
      totalScore:-5
    playerScores[1]:
      playerId:player-2
      totalScore:10
  <Game:GameEnded>
    winnerId:player-1
    finalScoreboard[0]:
      playerId:player-1
      totalScore:-5
      rank:1
    finalScoreboard[1]:
      playerId:player-2
      totalScore:10
      rank:2
---
