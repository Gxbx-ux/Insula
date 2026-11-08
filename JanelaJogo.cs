using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace JogoDasIlhas;

public sealed class JanelaJogo : Form
{
    private const int Linhas = 8;
    private const int Colunas = 14;
    private readonly Peca?[,] tabuleiro = new Peca?[Linhas, Colunas];
    private readonly PainelTabuleiro painel = new();
    private readonly Label estado = new();
    private readonly Label ajuda = new();
    private readonly Button reiniciar = new();
    private int jogador = 1;
    private (int linha, int coluna)? selecionada;
    private List<(int linha, int coluna)> destinos = new();
    private bool terminado;

    private enum Tipo { Atacante, Colonizador, Reforco }
    private sealed record Peca(int Jogador, Tipo Tipo);

    public JanelaJogo()
    {
        Text = "Jogo das Ilhas";
        ClientSize = new Size(1050, 720);
        MinimumSize = new Size(780, 570);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 248, 245);

        var cabecalho = new Panel { Dock = DockStyle.Top, Height = 92, Padding = new Padding(16, 10, 16, 8) };
        estado.AutoSize = true;
        estado.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        estado.Location = new Point(16, 10);
        ajuda.AutoSize = true;
        ajuda.Font = new Font("Segoe UI", 9);
        ajuda.ForeColor = Color.FromArgb(65, 77, 73);
        ajuda.Location = new Point(17, 46);
        ajuda.Text = "A = atacante   C = colonizador   R = reforço     •     Clique numa peça e depois numa casa destacada";
        reiniciar.Text = "Novo jogo";
        reiniciar.AutoSize = true;
        reiniciar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        reiniciar.Location = new Point(cabecalho.Width - 120, 12);
        reiniciar.Click += (_, _) => NovoJogo();
        cabecalho.Controls.AddRange(new Control[] { estado, ajuda, reiniciar });
        cabecalho.Resize += (_, _) => reiniciar.Left = cabecalho.ClientSize.Width - reiniciar.Width - 16;

        painel.Dock = DockStyle.Fill;
        painel.Paint += DesenharTabuleiro;
        painel.MouseClick += ClicarTabuleiro;
        var rodape = new Label
        {
            Dock = DockStyle.Bottom, Height = 55, TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(65, 77, 73),
            Text = "Azul começa em cima  •  Vermelho começa em baixo  •  Todas as peças podem recuar\nVer README.md para as regras e pressupostos."
        };
        Controls.Add(painel);
        Controls.Add(rodape);
        Controls.Add(cabecalho);
        NovoJogo();
    }

    // Colunas 3, 7 e 11 (numeração humana) são água.
    private static int Ilha(int coluna) => coluna switch
    {
        0 or 1 => 0,
        >= 3 and <= 5 => 1,
        >= 7 and <= 9 => 2,
        >= 11 and <= 13 => 3,
        _ => -1
    };
    private static bool Dentro(int l, int c) => l >= 0 && l < Linhas && c >= 0 && c < Colunas;

    private void NovoJogo()
    {
        Array.Clear(tabuleiro, 0, tabuleiro.Length);
        for (int c = 0; c < Colunas; c++)
        {
            if (Ilha(c) < 0) continue;
            // Da esquerda para a direita, os colonizadores começam nas ilhas:
            // azul, vermelho, azul, vermelho. Cada equipa tem apenas dois.
            // No lugar dos outros colonizadores ficam reforços.
            bool primeiraColuna = c == 0 || c == 3 || c == 7 || c == 11;
            bool colonizadorAzul = primeiraColuna && Ilha(c) % 2 == 0;
            bool colonizadorVermelho = primeiraColuna && Ilha(c) % 2 == 1;
            tabuleiro[0, c] = new Peca(1, colonizadorAzul ? Tipo.Colonizador : Tipo.Reforco);
            tabuleiro[1, c] = new Peca(1, Tipo.Atacante);
            tabuleiro[7, c] = new Peca(2, colonizadorVermelho ? Tipo.Colonizador : Tipo.Reforco);
            tabuleiro[6, c] = new Peca(2, Tipo.Atacante);
        }
        jogador = 1;
        selecionada = null;
        destinos.Clear();
        terminado = false;
        AtualizarEstado();
        painel.Invalidate();
    }

    private bool TemColonizador(int ilha, int equipa)
    {
        for (int l = 0; l < Linhas; l++)
            for (int c = 0; c < Colunas; c++)
                if (Ilha(c) == ilha && tabuleiro[l, c] is Peca p && p.Tipo == Tipo.Colonizador && p.Jogador == equipa)
                    return true;
        return false;
    }

    private List<(int linha, int coluna)> Movimentos(int linha, int coluna)
    {
        var resultado = new List<(int, int)>();
        Peca? p = tabuleiro[linha, coluna];
        if (p is null) return resultado;
        // O recuo é permitido para ambos os jogadores. Os atacantes também
        // se podem mover nas diagonais em ambos os sentidos.
        (int dl, int dc)[] direcoes = p.Tipo == Tipo.Atacante
            ? new[] { (-1, 0), (1, 0), (-1, -1), (-1, 1), (1, -1), (1, 1) }
            : new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

        foreach (var (dl, dc) in direcoes)
        {
            int ilhaAnterior = Ilha(coluna);
            int l = linha + dl, c = coluna + dc;
            int distancia = 1;
            int alcance = p.Tipo == Tipo.Colonizador ? int.MaxValue : 3;
            while (Dentro(l, c) && distancia <= alcance)
            {
                int ilhaAtual = Ilha(c);
                Peca? ocupante = tabuleiro[l, c];

                if (p.Tipo == Tipo.Atacante && ilhaAtual != Ilha(coluna)) break;

                if (p.Tipo == Tipo.Reforco && ilhaAtual >= 0 && ilhaAnterior == -1)
                {
                    // Ao entrar numa ilha a partir da água, a primeira casa tem
                    // de estar vazia e já tem de existir lá um colonizador da equipa do reforço.
                    if (ocupante is not null || !TemColonizador(ilhaAtual, p.Jogador)) break;
                }

                if (ocupante is not null)
                {
                    // Todas as peças terminam o movimento em terra. Na água,
                    // uma peça existente bloquearia a passagem sem ser capturada.
                    if (ocupante.Jogador != p.Jogador && ilhaAtual >= 0)
                        resultado.Add((l, c));
                    break; // Nunca se salta por cima de uma peça.
                }

                // Colonizadores e reforços podem atravessar água, mas nenhuma
                // peça pode terminar o movimento na água.
                if (ilhaAtual >= 0)
                    resultado.Add((l, c));
                ilhaAnterior = ilhaAtual;
                l += dl;
                c += dc;
                distancia++;
            }
        }
        return resultado;
    }

    private Rectangle AreaTabuleiro()
    {
        int tamanho = Math.Max(1, Math.Min((painel.ClientSize.Width - 30) / Colunas, (painel.ClientSize.Height - 20) / Linhas));
        return new Rectangle((painel.ClientSize.Width - tamanho * Colunas) / 2,
            (painel.ClientSize.Height - tamanho * Linhas) / 2, tamanho * Colunas, tamanho * Linhas);
    }

    private void ClicarTabuleiro(object? sender, MouseEventArgs e)
    {
        if (terminado || e.Button != MouseButtons.Left) return;
        Rectangle area = AreaTabuleiro();
        if (!area.Contains(e.Location)) return;
        int tamanho = area.Width / Colunas;
        int c = (e.X - area.X) / tamanho, l = (e.Y - area.Y) / tamanho;

        if (selecionada is { } origem && destinos.Contains((l, c)))
        {
            tabuleiro[l, c] = tabuleiro[origem.linha, origem.coluna];
            tabuleiro[origem.linha, origem.coluna] = null;
            selecionada = null;
            destinos.Clear();
            jogador = jogador == 1 ? 2 : 1;
            int pecasAzuis = 0, pecasVermelhas = 0, colonizadores = 0;
            bool temJogadas = false;
            for (int x = 0; x < Linhas; x++)
                for (int y = 0; y < Colunas; y++)
                    if (tabuleiro[x, y] is Peca peca)
                    {
                        if (peca.Jogador == 1) pecasAzuis++;
                        else pecasVermelhas++;
                        if (peca.Tipo == Tipo.Colonizador) colonizadores++;
                        if (peca.Jogador == jogador && Movimentos(x, y).Count > 0)
                            temJogadas = true;
                    }

            string? resultado = null;
            // A vitória tem prioridade: mesmo que a última peça capturada seja
            // um colonizador, a equipa sem peças perde.
            if (pecasAzuis == 0) resultado = "Vermelho ganhou!";
            else if (pecasVermelhas == 0) resultado = "Azul ganhou!";
            else if (colonizadores == 0)
                resultado = "Empate! Já não há colonizadores de nenhuma equipa.";
            else if (!temJogadas)
                resultado = "Empate! O jogador seguinte não tem jogadas possíveis.";

            if (resultado is not null)
            {
                terminado = true;
                estado.Text = resultado;
                painel.Invalidate();
                painel.Update(); // Mostrar a última jogada antes da mensagem.
                MessageBox.Show(this, resultado, "Fim do jogo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else AtualizarEstado();
        }
        else if (tabuleiro[l, c] is Peca p && p.Jogador == jogador)
        {
            selecionada = (l, c);
            destinos = Movimentos(l, c);
        }
        else
        {
            selecionada = null;
            destinos.Clear();
        }
        painel.Invalidate();
    }

    private void AtualizarEstado() => estado.Text = jogador == 1 ? "Vez do jogador azul" : "Vez do jogador vermelho";

    private void DesenharTabuleiro(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Rectangle area = AreaTabuleiro();
        int t = area.Width / Colunas;
        using var grelha = new Pen(Color.FromArgb(110, 255, 255, 255), 1);
        using var destaque = new SolidBrush(Color.FromArgb(130, 255, 234, 100));
        using var bordaSelecao = new Pen(Color.Gold, Math.Max(2, t / 18f));
        using var azul = new SolidBrush(Color.FromArgb(38, 92, 180));
        using var vermelho = new SolidBrush(Color.FromArgb(188, 54, 62));
        using var texto = new SolidBrush(Color.White);
        using var fonte = new Font("Segoe UI", Math.Max(10, t * 0.28f), FontStyle.Bold, GraphicsUnit.Pixel);
        using var centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        for (int l = 0; l < Linhas; l++)
            for (int c = 0; c < Colunas; c++)
            {
                var r = new Rectangle(area.X + c * t, area.Y + l * t, t, t);
                int ilha = Ilha(c);
                Color cor = ilha < 0
                    ? ((l + c) % 2 == 0 ? Color.FromArgb(82, 166, 202) : Color.FromArgb(68, 151, 191))
                    : ((l + c) % 2 == 0 ? Color.FromArgb(143, 191, 127) : Color.FromArgb(120, 174, 109));
                using (var fundo = new SolidBrush(cor)) g.FillRectangle(fundo, r);
                if (destinos.Contains((l, c))) g.FillRectangle(destaque, r);
                g.DrawRectangle(grelha, r);
                if (tabuleiro[l, c] is Peca p)
                {
                    int margem = Math.Max(4, t / 8);
                    var circulo = new Rectangle(r.X + margem, r.Y + margem, t - 2 * margem, t - 2 * margem);
                    g.FillEllipse(p.Jogador == 1 ? azul : vermelho, circulo);
                    string letra = p.Tipo switch { Tipo.Atacante => "A", Tipo.Colonizador => "C", _ => "R" };
                    g.DrawString(letra, fonte, texto, circulo, centro);
                }
                if (selecionada == (l, c))
                    g.DrawRectangle(bordaSelecao, r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4);
            }
        if (terminado && estado.Text.EndsWith(" ganhou!"))
        {
            using var sombra = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
            g.FillRectangle(sombra, area);
            using var fonteVitoria = new Font("Segoe UI", Math.Max(24, t * 0.55f), FontStyle.Bold, GraphicsUnit.Pixel);
            using var tinta = new SolidBrush(estado.Text.StartsWith("Azul") ? Color.FromArgb(38, 92, 180) : Color.FromArgb(188, 54, 62));
            g.DrawString(estado.Text, fonteVitoria, tinta, area, centro);
        }
    }

    private sealed class PainelTabuleiro : Panel
    {
        public PainelTabuleiro() { DoubleBuffered = true; ResizeRedraw = true; }
    }
}
