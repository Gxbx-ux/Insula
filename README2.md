# Jogo das Ilhas

Jogo de tabuleiro para dois jogadores locais numa janela C# Windows Forms.

## Como executar

É necessário o Windows e o SDK .NET 8 (ou superior). Na pasta deste projeto:

```bash
dotnet run
```

Também podes abrir `JogoDasIlhas.csproj` no Visual Studio com a carga de trabalho de desenvolvimento de aplicações .NET para ambiente de trabalho e premir F5.

## Como jogar

O tabuleiro tem 8 linhas e 14 colunas: quatro ilhas verdes separadas por três faixas de água. O azul começa no topo e o vermelho em baixo. Cada equipa começa com dois colonizadores: da esquerda para a direita, as ilhas começam com um colonizador azul, vermelho, azul e vermelho, respetivamente. O azul joga primeiro. Clica numa peça tua e depois numa das casas amarelas para a mover. Uma peça adversária na casa de destino é capturada. Nenhuma peça pode saltar por cima de outra. O botão **Novo jogo** reinicia a partida.

- **A — Atacante:** move-se até 3 casas para a frente, para trás ou em diagonal (nos dois sentidos), sem sair da sua ilha. Não se move lateralmente na horizontal.
- **C — Colonizador:** move-se qualquer número de casas na vertical ou horizontal. Pode atravessar água e passar por várias ilhas, mas tem de terminar o movimento em terra.
- **R — Reforço:** move-se até 3 casas na vertical ou horizontal. Pode atravessar água para mudar de ilha, mas só termina o movimento em terra. Para entrar numa ilha, já tem de haver nessa ilha pelo menos um colonizador da mesma equipa que o reforço e a primeira casa de terra tem de estar vazia; ela pode estar sob ataque adversário. Depois de entrar pode capturar numa casa posterior do mesmo movimento. Estas condições aplicam-se a cada ilha atravessada.

Ganha quem capturar todas as peças adversárias. Aparece uma mensagem no ecrã com o vencedor. Se ambas as equipas ainda tiverem peças e já não houver colonizadores de nenhuma das equipas, o jogo termina empatado. Também há empate se o jogador seguinte não tiver movimentos legais. A vitória por capturar a última peça tem prioridade sobre o empate.

## Decisões assumidas

Como alguns detalhes não estavam especificados, foram adotadas estas regras: dois jogadores, os atacantes podem recuar e mover-se nas diagonais nos dois sentidos, movimentos sem limite de distância para colonizadores, colonizador da equipa do reforço na ilha de destino, e colonizadores e reforços sem poder parar na água. As capturas são feitas entrando na casa da peça adversária, sem saltar sobre ela. Se quiseres alterar alguma destas regras, o código dos movimentos está no método `Movimentos` de `JanelaJogo.cs`.
