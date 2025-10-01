document.addEventListener('DOMContentLoaded', () => {

    const boardSpaces = [
        // --- Cantos ---
        { id: 'space-0', name: 'Go', style: { top: '695px', left: '702px', width: '98px', height: '105px' } },
        { id: 'space-10', name: 'Jail', style: { top: '695px', left: '0px', width: '98px', height: '105px' } },
        { id: 'space-20', name: 'Free Parking', style: { top: '0px', left: '0px', width: '98px', height: '105px' } },
        { id: 'space-30', name: 'Go to Jail', style: { top: '0px', left: '702px', width: '98px', height: '105px' } },

        // --- Linha de baixo ---
        { id: 'space-1', name: 'Mediterranean Avenue', style: { top: '695px', left: '635px', width: '67px', height: '105px' } },
        { id: 'space-2', name: 'Community Chest', style: { top: '695px', left: '568px', width: '67px', height: '105px' } },
        { id: 'space-3', name: 'Baltic Avenue', style: { top: '695px', left: '501px', width: '67px', height: '105px' } },
        { id: 'space-4', name: 'Income Tax', style: { top: '695px', left: '434px', width: '67px', height: '105px' } },
        { id: 'space-5', name: 'Reading Railroad', style: { top: '695px', left: '367px', width: '67px', height: '105px' } },
        { id: 'space-6', name: 'Oriental Avenue', style: { top: '695px', left: '300px', width: '67px', height: '105px' } },
        { id: 'space-7', name: 'Chance', style: { top: '695px', left: '233px', width: '67px', height: '105px' } },
        { id: 'space-8', name: 'Vermont Avenue', style: { top: '695px', left: '166px', width: '67px', height: '105px' } },
        { id: 'space-9', name: 'Connecticut Avenue', style: { top: '695px', left: '98px', width: '67px', height: '105px' } },

        // --- Coluna da esquerda ---
        { id: 'space-11', name: 'St. Charles Place', style: { top: '634px', left: '0px', width: '98px', height: '66px' } },
        { id: 'space-12', name: 'Electric Company', style: { top: '568px', left: '0px', width: '98px', height: '66px' } },
        { id: 'space-13', name: 'States Avenue', style: { top: '502px', left: '0px', width: '98px', height: '66px' } },
        { id: 'space-14', name: 'Virginia Avenue', style: { top: '436px', left: '0px', width: '98px', height: '66px' } },
        { id: 'space-15', name: 'Pennsylvania Railroad', style: { top: '370px', left: '0px', width: '98px', height: '66px' } },
        { id: 'space-16', name: 'St. James Place', style: { top: '304px', left: '0px', width: '98px', height: '66px' } },
        { id: 'space-17', name: 'Community Chest', style: { top: '238px', left: '0px', width: '98px', height: '66px' } },
        { id: 'space-18', name: 'Tennessee Avenue', style: { top: '172px', left: '0px', width: '98px', height: '66px' } },
        { id: 'space-19', name: 'New York Avenue', style: { top: '106px', left: '0px', width: '98px', height: '66px' } },

        // --- Linha de cima ---
        { id: 'space-21', name: 'Kentucky Avenue', style: { top: '0px', left: '98px', width: '67px', height: '105px' } },
        { id: 'space-22', name: 'Chance', style: { top: '0px', left: '165px', width: '67px', height: '105px' } },
        { id: 'space-23', name: 'Indiana Avenue', style: { top: '0px', left: '232px', width: '67px', height: '105px' } },
        { id: 'space-24', name: 'Illinois Avenue', style: { top: '0px', left: '299px', width: '67px', height: '105px' } },
        { id: 'space-25', name: 'B. & O. Railroad', style: { top: '0px', left: '366px', width: '67px', height: '105px' } },
        { id: 'space-26', 'name': 'Atlantic Avenue', style: { top: '0px', left: '433px', width: '67px', height: '105px' } },
        { id: 'space-27', name: 'Ventnor Avenue', style: { top: '0px', left: '500px', width: '67px', height: '105px' } },
        { id: 'space-28', name: 'Water Works', style: { top: '0px', left: '567px', width: '67px', height: '105px' } },
        { id: 'space-29', name: 'Marvin Gardens', style: { top: '0px', left: '635px', width: '67px', height: '105px' } },

        // --- Coluna da direita ---
        { id: 'space-31', name: 'Pacific Avenue', style: { top: '106px', left: '702px', width: '98px', height: '66px' } },
        { id: 'space-32', name: 'North Carolina Avenue', style: { top: '172px', left: '702px', width: '98px', height: '66px' } },
        { id: 'space-33', name: 'Community Chest', style: { top: '238px', left: '702px', width: '98px', height: '66px' } },
        { id: 'space-34', name: 'Pennsylvania Avenue', style: { top: '304px', left: '702px', width: '98px', height: '66px' } },
        { id: 'space-35', name: 'Short Line', style: { top: '370px', left: '702px', width: '98px', height: '66px' } },
        { id: 'space-36', name: 'Chance', style: { top: '436px', left: '702px', width: '98px', height: '66px' } },
        { id: 'space-37', name: 'Park Place', style: { top: '502px', left: '702px', width: '98px', height: '66px' } },
        { id: 'space-38', name: 'Luxury Tax', style: { top: '568px', left: '702px', width: '98px', height: '66px' } },
        { id: 'space-39', name: 'Boardwalk', style: { top: '634px', left: '702px', width: '98px', height: '66px' } },

        // --- MONTES DE CARTAS (AJUSTADOS) ---
        { id: 'community-chest-pile', name: 'Monte de Cartas Cofre', style: { top: '175px', left: '145px', width: '140px', height: '92px', transform: 'rotate(-45deg)' } },
        { id: 'chance-pile', name: 'Monte de Cartas Sorte', style: { top: '535px', left: '515px', width: '140px', height: '92px', transform: 'rotate(-45deg)' } },
    ];

    const gameBoardContainer = document.getElementById('game-board-container');

    function createBoard() {
        gameBoardContainer.innerHTML = ''; 

        boardSpaces.forEach(spaceData => {
            const spaceElement = document.createElement('div');
            
            spaceElement.id = spaceData.id;
            spaceElement.className = 'board-space';

            Object.assign(spaceElement.style, spaceData.style);

            spaceElement.addEventListener('click', () => {
                console.log('id: ' + spaceData.id);
            });
            
            gameBoardContainer.appendChild(spaceElement);
        });
    }

    createBoard();
});