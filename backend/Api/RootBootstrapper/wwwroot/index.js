API_URL = 'http://localhost:3000';
userId = 'local'
const service = new HorusService(API_URL);

var connection = new signalR.HubConnectionBuilder().withUrl("/chatsessions").build();
var typingId = null;
var loadingChatId = null;
var typingMessageIdInterval = null;
var enableScrollOnChatContainer = true;
let isUserScrolledUp = false;
const SCROLL_THRESHOLD = 10;
var isSideBarOpened = false;
let currentSessionToDelete = null;
var currentSessionToRename = null;
var isAssistantTyping = false;

// Função para mostrar/esconder o modal
const toggleDeleteModal = (show = true) => {
  $('#deleteModal').toggleClass('hidden', !show);
};

const displayMessage = (role, content) => {
    const messageElement = document.createElement('div');
    const chatContainer  =document.querySelector('.chat-container');

    messageElement.classList.add('message', role === 'user' ? 'user-message' : 'assistant-message');
    messageElement.textContent = content;
    chatContainer.appendChild(messageElement);
    chatContainer.scrollTop = chatContainer.scrollHeight;
};
var previousText = '';

const toggleInputForRename = (show = true, sessionId) => {
    const el  = $('.chat-list > div[data-id=\''+sessionId+'\'] > div > span');
    if(show){
        previousText = el.text();
        el.html(`<input type='text' class='bg-[#171717]' value='${previousText}'/>`)
    }
    else
    {
        el.text(previousText)
    }
        
  };

// Função principal de delete
function deleteChatSession(sessionId) {
  currentSessionToDelete = sessionId;
  toggleDeleteModal(true);
}

function renameChatSession(sessionId) {
    currentSessionToRename = sessionId;
    toggleInputForRename(true, currentSessionToRename);
  }

function startNewChat(){
        const inputContainer = document.getElementById('input-container');
        const chatContainer  =document.querySelector('.chat-container');
        window.history.pushState({}, '', '/');
        chatContainer.innerHTML = '';
        $('#chat-title').text('New Chat');
        chatContainer.classList.remove('flex-1');
        // inputContainer.classList.add('max-w-2xl')
        // inputContainer.classList.remove('max-w')
        const isSmall = window.innerWidth <= 640;
            if(isSmall && isSideBarOpened)
                toggleSideBar();
}



const changeChatTitle = (title) => {
    const chatTileEl = document.getElementById('chat-title');
    chatTileEl.textContent = '';
    let i = 0;
    clearInterval(typingId);
    typingId = setInterval(() => {
        chatTileEl.textContent += title.charAt(i);
        i++;

        if (i === title.length) {
            clearInterval(typingId);
        }
    }, 150);
};


const toggleSideBar = function () {
    const mainContainer = document.getElementById('main-container');
    const sidebar = document.getElementById('sidebar');
    console.info('contém a classe -translate-x-full?', sidebar.classList.contains('-translate-x-full'));
    mainContainer.classList.toggle('translate-x-64');
    mainContainer.classList.toggle('lg:translate-x-0');
    mainContainer.classList.toggle('mr-[calc((100%-18rem)/2)]');
    mainContainer.classList.toggle('sm:mr-[34%]');
    mainContainer.classList.toggle('md:mr-[28%]');
    // mainContainer.classList.toggle('lg:mr-auto');
    sidebar.classList.toggle('-translate-x-full');
    isSideBarOpened = !isSideBarOpened
}

connection.on("ReceiveTitleChanged", function (chatSessionId, title) {
    console.log(`Chat title changed for session ${chatSessionId}: ${title}`);
    $(`[data-id='${chatSessionId}']`).text(title);
    changeChatTitle(title);
});

connection.start().then(function () {
    console.info('SignalR Conectado');
}).catch(function (err) {
    return console.error(err.toString());
});

function testarEnvio(message) {
    connection.invoke("SendMessage", 'localhost', message).catch(function (err) {
        return console.error(err.toString());
    });
}



const removeLoading = (element) => {
    element.css('opacity', '0');
    setTimeout(() => element.remove(), 800);
};

const displayLoading = () => {
    const loadingElement = $(`
        <div class="message assistant-message flex items-center gap-2 bg-[#2A2A2A] text-gray-400 p-3 rounded-md">
            <i class="fas fa-spinner fa-spin"></i>
            <span>Thinking...</span>
        </div>
    `);

    $('.chat-container').append(loadingElement);
    $('.chat-container').scrollTop($('.chat-container')[0].scrollHeight);
    return loadingElement;
};

const displayMessageWithTypingEffect = (role, text) => {
    const messageContainer = document.createElement('div');
    messageContainer.classList.add('message', role === 'user' ? 'user-message' : 'assistant-message');
    const chatContainer = document.querySelector('.chat-container');

    chatContainer.appendChild(messageContainer);

    clearInterval(typingMessageIdInterval);
    enableScrollOnChatContainer = true;

    let i = 0;
    typingMessageIdInterval = setInterval(() => {
        messageContainer.textContent += text.charAt(i);
        i++;
        // Só faz scroll automático se o usuário não estiver scrollado para cima
        if (!isUserScrolledUp) {
            chatContainer.scrollTop = chatContainer.scrollHeight;
        }


        if (i === text.length) {
            clearInterval(typingMessageIdInterval);
            const isNearBottom = chatContainer.scrollTop + chatContainer.clientHeight
                >= chatContainer.scrollHeight - SCROLL_THRESHOLD;
            if (isNearBottom) {
                chatContainer.scrollTop = chatContainer.scrollHeight;
            }
        }
    }, 30); 
};

const sendMessage = async () => {
    const messageInput = document.getElementById('message-input');
    const sendBtnIcon = document.querySelector('#sendBtn i');
    const chatContainer = document.querySelector('.chat-container');
    const sendButton = messageInput.nextElementSibling;
    const text = messageInput.value.trim();

    if(isAssistantTyping)
    {
        messageInput.value = '';
        messageInput.style.height = 'auto';
        sendBtnIcon.classList.remove('fa-stop');
        sendBtnIcon.classList.add('fa-arrow-up');
        isAssistantTyping = false;
        clearInterval(typingMessageIdInterval)
        return;
    }

    chatContainer.classList.add('flex-1');
    messageInput.style.height = 'auto';
    sendBtnIcon.classList.add('fa-stop');
    sendBtnIcon.classList.remove('fa-arrow-up');
    // Obter ID da sessão do chat a partir da URL
    const pathSegments = window.location.pathname.split('/');
    const chatSessionId = pathSegments[2] || null;

    if (text) {
        try {
            // Desabilita o input durante o processamento
            messageInput.disabled = true;
            sendButton.disabled = true;

            displayMessage('user', text);
            messageInput.value = '';

            const loadingElement = displayLoading();




            const response = await service.sendMessage(text, userId, chatSessionId);

            // Modificação principal aqui ↓
            if (response.chatSessionId && !chatSessionId) {
                // Extrair as mensagens atuais como dados serializáveis
                const messages = Array.from(chatContainer.children).map(child => ({
                    role: child.classList.contains('user') ? 'user' : 'assistant',
                    text: child.textContent.trim()
                }));

                const newState = {
                    chatSessionId: response.chatSessionId,
                    messages: messages // Array de objetos simples
                };
                window.history.pushState(newState, '', `/chat/${response.chatSessionId}`);
            }

            removeLoading(loadingElement);
            isAssistantTyping = true
            displayMessageWithTypingEffect('assistant', response.text);

            if (chatSessionId == null) {
                loadChatSessions();
            }

        } catch (error) {
            console.error('Erro:', error);
            removeLoading(loadingElement);
            displayMessage('assistant', 'Desculpe, ocorreu um erro ao processar sua solicitação.');
        }
        finally {
            // Reabilita o input independente de sucesso/erro
            messageInput.disabled = false;
            sendButton.disabled = false;
        }
    }
};

document.addEventListener('DOMContentLoaded', function () {
    // Toggle sidebar
    const sidebar = document.getElementById('sidebar');
    const menuToggle = document.getElementById('menuToggle');
    // Message input handling
    const messageInput = document.getElementById('message-input');
    const sendButton = messageInput.nextElementSibling;
    const chatContainer = document.querySelector('.chat-container');
    const inputContainer = document.getElementById('input-container');

    // Evento de confirmação
$('#confirmDelete').on('click', async () => {
    if (!currentSessionToDelete) return;
  
    try {
        $('#confirmDelete').prop('disabled', true).html(`
            <div class="flex items-center">
              <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
              Deleting...
            </div>
          `);
      // Substitua pela sua chamada API real
      await service.deleteChatSession(currentSessionToDelete);
    setTimeout(() => {
        toggleDeleteModal(false);
        // Atualiza a lista
        loadChatSessions();
        $("#sendBtn i").removeClass("fa-stop").addClass("fa-arrow-up");
        startNewChat()
            $('#confirmDelete').prop('disabled', false).text('Delete');
        }, 3_000);
    
    } catch (error) {
      console.error('Delete error:', error);
    }
 


  });
  
  // Evento de cancelamento
  $('#cancelDelete').on('click', () => toggleDeleteModal(false));
  
  // Fechar modal ao clicar fora
  $('#deleteModal').on('click', (e) => {
    if ($(e.target).attr('id') === 'deleteModal') {
      toggleDeleteModal(false);
    }
  });
  
  // Fechar com ESC
  $(document).on('keydown', (e) => {
    if (e.key === 'Escape' && !$('#deleteModal').hasClass('hidden')) {
      toggleDeleteModal(false);
    }
  });

    chatContainer.addEventListener('scroll', () => {
        const isNearBottom = chatContainer.scrollTop + chatContainer.clientHeight
            >= chatContainer.scrollHeight - SCROLL_THRESHOLD;
        isUserScrolledUp = !isNearBottom;
    });

    const initialPath = window.location.pathname;
    if (initialPath.startsWith('/chat/')) {
        const sessionId = initialPath.split('/')[2];
        chatContainer.classList.add('flex-1');
        // inputContainer.classList.remove('max-w-2xl')
        // inputContainer.classList.add('max-w')
        loadChatSession(sessionId);
    }





    menuToggle.addEventListener('click', toggleSideBar);

    // Adicione isso no seu DOMContentrLoaded
    window.addEventListener('popstate', function (event) {
        if (event.state?.chatSessionId) {
            chatContainer.classList.add('flex-1');
            
            loadChatSession(event.state.chatSessionId);
        } else {
            // Lógica para estado inicial sem chat
            chatContainer.innerHTML = '';
            chatContainer.classList.remove('flex-1');
        }
    });







   

    // Função exemplo para carregar um chat específico
    async function loadChatSession(sessionId) {
        try {
            // Lógica para exibir o histórico no chat
            chatContainer.innerHTML = '';
            chatContainer.classList.add('flex-1');
            inputContainer.classList.remove('max-w-2xl')
            inputContainer.classList.add('max-w')
            $("#sendBtn i").removeClass("fa-stop").addClass("fa-arrow-up");
            const isSmall = window.innerWidth <= 640;
            if(isSmall && isSideBarOpened)
                toggleSideBar();
            const messages = await service.getChatHistory(sessionId);
            changeChatTitle(messages.title);

            const loadingElement = displayLoading();

            clearTimeout(loadingChatId);

            loadingChatId = setTimeout(() => {
                removeLoading(loadingElement);

                messages.messages.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt)).forEach(msg => {
                    displayMessage(msg.role, msg.content);
                })
            }, 1_000);

        } catch (error) {
            console.error('Erro ao carregar histórico:', error);
        }
    }

    sendButton.addEventListener('click', sendMessage);



    const loadChatSessions = async () => {
        try {
            const response = await service.getAllChatSessions({
                sort: "createdAt desc",
                offset: 1,
                pageSize: 10
            });

            updateChatList(response.items);
        } catch (error) {
            console.error('Erro ao carregar conversas:', error);
        }
    };

    const updateChatList = (sessions) => {
        const chatListContainer = document.querySelector('.flex-1.overflow-y-auto');
        chatListContainer.innerHTML = ''; // Limpa o conteúdo estático

        // Agrupa conversas por data
        const grouped = groupSessionsByDate(sessions);
        const groups = ['Today', 'Yesterday', 'Previous 7 days']

        // Cria os elementos do DOM
        groups
        .filter(d => Object.keys(grouped).includes(d))
        .forEach(dateGroup => {
            const groupElement = createDateGroupElement(dateGroup, grouped[dateGroup]);
            chatListContainer.appendChild(groupElement);
        });
    };


    const groupSessionsByDate = (sessions) => {
        const groups = {};

        sessions.forEach(session => {
            const date = new Date(session.createdAt).toLocaleDateString('pt-BR');
            const today = new Date().toLocaleDateString('pt-BR');
            const yesterday = new Date(Date.now() - 86400000).toLocaleDateString('pt-BR');

            let groupName;
            if (date === today) groupName = 'Today';
            else if (date === yesterday) groupName = 'Yesterday';
            else groupName = 'Previous 7 days';

            if (!groups[groupName]) groups[groupName] = [];
            groups[groupName].push(session);
        });

        return groups;
    };




    const createDateGroupElement = (title, sessions) => {
    const groupDiv = document.createElement('div');
    groupDiv.className = 'mb-4';

    // Header (mantido igual)
    const header = document.createElement('div');
    header.className = 'px-4 py-2 text-sm text-gray-400 flex items-center cursor-pointer';
    header.onclick = () => {
        const itemsContainer = document.getElementById(`chat-list-${title}`);
        const arrow = document.querySelector(`.arrow-${title}`);
        
        // Alternar visibilidade
        itemsContainer.classList.toggle('opacity-0');
        itemsContainer.classList.toggle('max-h-0');
        itemsContainer.classList.toggle('invisible');
        
        arrow.classList.toggle('rotate-0');

        // Aplicar animação nos itens somente quando abrir
        if (!itemsContainer.classList.contains('opacity-0')) {
            const items = itemsContainer.children;
            Array.from(items).forEach((item, index) => {
                item.style.animationDelay = `${index * 0.1}s`;
                void item.offsetWith;
                item.classList.add('animate-fade-in-up');
            });
        }else {
            Array.from(itemsContainer.children).forEach(item => {
                item.classList.remove('animate-fade-in-up');
                item.style.opacity = '0'; // Forçar reset da opacidade
            });
        }

    };
    header.innerHTML = `
        <i class="arrow arrow-${title} fas fa-chevron-up w-4 h-4 mr-1 rotate-180 duration-300"></i>
        ${title}
    `;

    // Container dos itens (modificado)
    const itemsContainer = document.createElement('div');
    itemsContainer.className = 'chat-list transition-visibility opacity-0 max-h-0 invisible'; // Inicialmente oculto
    itemsContainer.id = `chat-list-${title}`;
    

    sessions.forEach((session, index) => {
        const item = document.createElement('div');
        item.className = 'px-4 py-2 text-sm hover:bg-[#2A2A2A] cursor-pointer opacity-0 relative group/item'; // Inicialmente transparente
       // Container do texto e ícone
       const contentDiv = document.createElement('div');
       contentDiv.className = 'flex justify-between items-center';
       
       // Texto da conversa
       const textSpan = document.createElement('span');
       textSpan.style.textOverflow = 'ellipsis'
       textSpan.style.width = '10rem'
       textSpan.style.overflow = 'hidden'
       textSpan.style.textWrap = 'nowrap'
       textSpan.textContent = session.title || 'Nova conversa';
       
       // Ícone de três pontos
       const menuIcon = document.createElement('div');
       menuIcon.className = 'opacity-0 group-hover/item:opacity-100 transition-opacity p-1 hover:bg-[#3A3A3A] rounded-md';
       menuIcon.innerHTML = `
           <svg class="w-4 h-4 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
               <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01"/>
           </svg>
       `;

       // Menu de contexto
       const contextMenu = document.createElement('div');
       contextMenu.className = 'hidden absolute right-0 top-6 bg-[#2A2A2A] rounded-md shadow-lg z-100';
       contextMenu.innerHTML = `
           <div class="py-1 text-sm text-gray-300 min-w-[120px]">
               <div class="px-3 py-2 hover:bg-[#3A3A3A] cursor-pointer">Delete</div>
               <div class="px-3 py-2 hover:bg-[#3A3A3A] cursor-pointer">Rename</div>
           </div>
       `;

       // Eventos do menu
       menuIcon.addEventListener('click', (e) => {
           e.stopPropagation(); // Impede o evento de chegar no item principal
           contextMenu.classList.toggle('hidden');
           
           // Fechar menu ao clicar fora
           const clickHandler = (event) => {
               if (!contextMenu.contains(event.target) && !menuIcon.contains(event.target)) {
                   contextMenu.classList.add('hidden');
                   document.removeEventListener('click', clickHandler);
               }
           };
           document.addEventListener('click', clickHandler);
       });

       // Evento de deletar
       contextMenu.querySelector('div:first-child').addEventListener('click', () => {
           deleteChatSession(session.id); // Você precisa implementar esta função
       });

       // Evento de deletar
       contextMenu.querySelector('div:nth-child(2)').addEventListener('click', () => {
        renameChatSession(session.id); // Você precisa implementar esta função
    });


       // Montagem dos elementos
       contentDiv.appendChild(textSpan);
       contentDiv.appendChild(menuIcon);
       contentDiv.appendChild(contextMenu);
       item.appendChild(contentDiv);
       item.dataset.id = session.id;
        
        item.addEventListener('click', () => {
            window.history.pushState(
                { chatSessionId: session.id },
                '',
                `/chat/${session.id}`
            );
            loadChatSession(session.id);
        });
        
        itemsContainer.appendChild(item);
    });

    groupDiv.appendChild(header);
    groupDiv.appendChild(itemsContainer);
    return groupDiv;
};

    document.querySelector('#newChatBtn').addEventListener('click', startNewChat);

    loadChatSessions();


});


    function checkSend(event) {
        console.info(event.key)
        console.info(event.shiftKey)
        // Se for Enter e não estiver segurando Shift, envia a mensagem
        if (event.key === 'Enter' && !event.shiftKey) {
          event.preventDefault(); // Evita pular a linha
          sendMessage();
        }
      }

 // Ajusta automaticamente a altura do textarea
 function autoResize(textarea) {
    textarea.style.height = 'auto';
    textarea.style.height = textarea.scrollHeight + 'px';
  }

  // Verifica se o usuário apertou Enter sem Shift
