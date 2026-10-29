# Costa Brava

Protótipo de ação 3D em terceira pessoa, feito no projeto **MEUGTAIA**, Unity 6000.6.4f1 / URP.

Abra `Assets/CostaBrava/Scenes/CostaBrava.unity`, pressione **Play** e depois **Enter**. A cena já fica configurada como primeira cena do build. O menu **Tools > Costa Brava** permite validar ou reconstruir a cidade. Reconstruir substitui a cena gerada; preserve uma cópia antes de editar o cenário manualmente.

## Controles

| Ação | Controle |
|---|---|
| Movimento / câmera | WASD / mouse |
| Caminhar / sprint | Ctrl / Shift |
| Pular | Espaço |
| Interagir, entrar e sair do carro | E |
| Pistola / carabina / rifle comprado | 1 / 2 / 3 |
| Soco / chute | F / G (LMB também dá socos com mãos livres) |
| Avançar horário em 3 horas | T |
| Guardar ou equipar arma | Q |
| Mirar / atirar | Botão direito / esquerdo do mouse |
| Recarregar | R |
| Acelerar / frear / ré | W / S |
| Direção / freio de mão | A e D / Espaço |
| Pausa / fechar loja | Esc |

Pare o veículo para sair. Há uma van na garagem a oeste da praça, um esportivo perto do início, SUVs e trânsito nas avenidas. O objetivo atual tem um marcador com distância na tela.

## Campanha curta

Interaja com os três marcadores dourados na Praça do Sol. Podem ser feitos em qualquer ordem.

1. **Entrega expressa:** entre na van da garagem, leve-a ao Café Maré e estacione no marcador azul. Recompensa: $300.
2. **Carga perdida:** elimine três guardas no pátio do porto, recolha a caixa laranja, despiste a polícia e chegue ao abrigo verde. Recompensa: $500.
3. **Última saída:** comece com duas estrelas, fique 18 segundos fora da visão policial e volte ao abrigo verde. Recompensa: $400.

A loja VIVA, na praça, vende munição para pistola ($50), carabina ($100) e recuperação de vida ($75). Você começa com $200. Morrer custa até $50; missões concluídas são mantidas durante a sessão, e a missão atual pode ser reiniciada no marcador. Ao concluir as três missões, a campanha termina e a exploração continua.

## Sistemas

`Assets/CostaBrava/Scripts` separa movimento, câmera, vida, combate, veículos, NPCs, wanted, missões, dinheiro, loja, áudio, efeitos e HUD. Armas usam ScriptableObjects. Pedestres, guardas e policiais usam NavMesh. Trânsito segue waypoints e detecta obstáculos. Câmera tem colisão, zoom de mira e FOV por velocidade. Efeitos, sons de combate e marcas de pneu têm pools limitados. A cidade utiliza materiais com instancing, batching estático e descarte por distância dos prédios e NPCs.

`SourceArt/CostaBrava_Props.blend` contém os modelos originais editáveis: quiosque VIVA, palmeira, banco e monumento. Os scripts de exportação e composição musical ficam em `Tools`. Créditos e licenças estão em `Documentation/ASSETS.md` e junto aos pacotes importados.

## Limites do protótipo

Física e IA arcade; policiais usam perseguição simples. A cidade tem oito quarteirões, fachadas decorativas e locais de interação externos. A campanha e o dinheiro não são persistidos entre sessões. Suspensão por quatro sondas de chão e áudio de motor sintetizado são simplificados. As animações de locomação e morte vêm de Kenney; poses de salto e queda complementam o conjunto. Não há interiores nem rádio. Há minimapa com objetivo ativo e um ciclo de dia/noite de 12 minutos. Occlusion Culling pode ser recalculado por Tools > Costa Brava > Finalizar apresentação e oclusão após mudanças no cenário.

Não é uma reprodução visual de GTA: marcas, personagens e identidade da cidade são fictícios.


## Expansão FAROL

Dois quarteirões a leste ampliam a cidade. A loja **FAROL / ARMAS**, no novo quarteirão sudeste, vende exclusivamente o **Rifle Farol com luneta por $100**. O rifle começa bloqueado, ocupa o espaço **3** depois da compra e recebe 5 cartuchos no carregador mais 20 de reserva. Munição adicional: 15 por $35 na FAROL. A loja VIVA continua vendendo apenas seus suprimentos originais.

Segure o botão direito com o rifle equipado para usar a luneta; a câmera muda o zoom e a sensibilidade. **F** dá socos, com até três golpes em sequência; **G** dá um chute mais forte. Os golpes causam dano e empurrão, e guardam a arma automaticamente.

Carros acumulam dano por tiros. Quando explodem, tornam-se destroços permanentes e não aceitam motorista. A explosão causa dano em área, lança props físicos e provoca tremor de câmera próximo. Barris, caixas e lixeiras podem ser destruídos. Se a van da entrega for destruída, a missão é interrompida e uma nova van de serviço fica disponível ao tentar novamente no marcador.

Os guardas hostis do porto aparecem somente durante a missão Carga perdida. Pedestres, trânsito e patrulhas comuns continuam na cidade. Reforços policiais aguardam locais distantes que estejam fora do campo de visão ou completamente encobertos; não há teleporte de viatura para um ponto fixo perto do jogador.

O minimapa mantém o norte para cima, indica a direção do jogador, as lojas e o alvo da missão ativa. Objetivos distantes ficam na borda. O ciclo de 12 minutos altera sol, ambiente e céu; postes e faróis ligam durante a noite. **T** permite avançar três horas para experimentar a iluminação. Motion blur aumenta suavemente durante aceleração em alta velocidade; FOV, poeira e marcas de pneus acompanham a condução.

As fontes editáveis novas ficam em `SourceArt/CostaBrava_Expansion.blend`. A atualização da cena preserva os objetos existentes e gera cópias em `Documentation/Backups`. Use **Tools > Costa Brava > Instalar expansão Farol na cena atual** para aplicar a atualização a uma cópia da cena original. Não é necessário reconstruir a cidade.
