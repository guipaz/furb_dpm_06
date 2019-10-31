const express = require('express');
const bodyParser = require('body-parser');
const app = express();
const generateGuid = require('uuid/v1');

app.use(bodyParser.json());
app.use(bodyParser.urlencoded({
  extended: true
}));

let games = {};

var tickLengthMs = 1000 / 1; // 1fps
var previousTick = Date.now()
function gameLoop() {
	var now = Date.now();
	if (previousTick + tickLengthMs <= now) {
		var delta = (now - previousTick) / 1000
		previousTick = now

		update(delta)
	}

	setTimeout(gameLoop)
}
setTimeout(gameLoop)

function update() {
	Object.keys(games).forEach((key) => {
		const game = games[key];
		updateGame(game);
	});
}

function updateGame(game) {
	const players = game.players;
	const now = Date.now();
	Object.keys(players).forEach((key) => {
		const player = players[key];
		const keepAlive = player.keepAlive;
		if (keepAlive + 10000 <= now) {
			console.log("Jogador desconectado: " + key);
			delete game.players.key;
		}
	});

	if (game.question && game.question.finishTime >= Date.now()) {
		game.finished = true;
	}
}

function isExistingGame(id) {
	return Object.keys(games).filter(g => g == id).length > 0;
}

function ok(res, data) {
	res.send({status: 200, data: data});
}

function error(res, error) {
	res.send({status: 500, error: error});
}

app.post('/games', function (req, res) {
	console.log(req.body);
	
	const id = req.body.id;
	if (isExistingGame(id)) {
		error(res, "Já existe um jogo com este identificador");
		return;
	}

	const secret = generateGuid();
	const game = { id: id, players: {}, secret: secret };
	games[req.body.id] = game;

	ok(res, { secret: secret });
});

app.post('/games/:id/keepAlive', function (req, res) {
	const id = req.params.id;
	const guid = req.body.guid;

	if (!isExistingGame(id)) {
		error(res, "Jogo não existente");
		return;
	}

	const now = Date.now();
	const game = games[id];
	const player = game.players[guid];
	if (!player) {
		error(res, "Jogador inexistente");
	}

	player.keepAlive = Date.now();
	ok(res);
});

app.post('/enterGame', function(req, res) {
	const id = req.body.id;
	const teamName = req.body.teamName;

	if (!isExistingGame(id)) {
		error(res, "Jogo não existente");
		return;
	}

	const player = {
		guid: generateGuid(),
		teamName: teamName,
		keepAlive: Date.now()
	};

	console.log(player);

	const game = games[id];
	game.players[teamName] = player;

	ok(res);
})

app.get('/state/:id', function(req, res) {
	const id = req.params.id;
	if (!isExistingGame(id)) {
		error("Jogo não existente");
		return;
	}

	const game = games[id];

	ok(res, { started: game.currentQuestion != undefined, currentQuestion: game.currentQuestion, finished: game.finished });
});

app.post('/sendQuestion/:id', function(req, res) {
	const id = req.params.id;
	const question = {
		operatorA: req.body.operatorA,
		operatorB: req.body.operatorB,
		operation: req.body.operation,
		options: req.body.options,
		finishTime: Date.now() + 10000,
		finished: false
	};

	console.log(question);

	games[id].currentQuestion = question;
	ok(res);
});

app.get('/games', function (req, res) {
	const response = { games: Object.keys(games).map((g) => 
	{
		return {id: g};
	})};
	ok(res, response);
});

const port = process.env.PORT || 3000;
app.listen(port, function () {
	console.log(`Server listening on port ${port}`);
});