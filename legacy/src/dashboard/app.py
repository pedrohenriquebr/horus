"""Dash application for the Horus bot monitoring dashboard."""
import dash
from dash import html, dcc, dash_table
from dash.dependencies import Input, Output, State
import plotly.graph_objs as go
from plotly.subplots import make_subplots
import requests
import json
from datetime import datetime, timedelta
import pandas as pd
import os
from typing import Dict, List
import dash_bootstrap_components as dbc

# Initialize the Dash app
app = dash.Dash(__name__, 
                title='Horus Bot Dashboard',
                meta_tags=[{'name': 'viewport',
                           'content': 'width=device-width, initial-scale=1.0'}],
                external_stylesheets=[dbc.themes.BOOTSTRAP])

# API endpoint
API_BASE_URL = "http://localhost:8000"

# Layout
app.layout = html.Div([
    # Header
    html.Div([
        html.H1('Horus Bot Dashboard',
                style={'textAlign': 'center'}),
        html.Div(id='bot-status',
                 style={'textAlign': 'center',
                       'marginBottom': '20px'})
    ]),
    
    # Control Panel
    html.Div([
        html.H3('Bot Control'),
        html.Button('Start', id='start-button', n_clicks=0,
                   style={'marginRight': '10px'}),
        html.Button('Stop', id='stop-button', n_clicks=0,
                   style={'marginRight': '10px'}),
        html.Button('Pause', id='pause-button', n_clicks=0),
        html.Div(id='control-status')
    ], style={'padding': '20px', 'backgroundColor': '#f8f9fa', 'marginBottom': '20px'}),
    
    # Tabs Container
    dcc.Tabs([
        # Tab 1: Overview
        dcc.Tab(label='Overview', children=[
            html.Div([
                # Message Processing Stats
                html.Div([
                    html.H3('Message Processing'),
                    dcc.Graph(id='message-processing-graph'),
                ], className='dashboard-card'),
                
                # Memory Operations
                html.Div([
                    html.H3('Memory Operations'),
                    html.Div([
                        html.Div(id='memory-stats'),
                        dcc.Graph(id='memory-operations-graph'),
                        dcc.Graph(id='memory-distribution-graph')
                    ], className='stats-container')
                ], className='dashboard-card'),
                
                # Resource Usage
                html.Div([
                    html.H3('Resource Usage'),
                    dcc.Graph(id='resource-usage-graph'),
                ], className='dashboard-card')
            ], style={'display': 'grid',
                     'gridTemplateColumns': 'repeat(auto-fit, minmax(400px, 1fr))',
                     'gap': '20px',
                     'padding': '20px'})
        ]),
        
        # Tab 2: Usuários Ativos
        dcc.Tab(label='Usuários Ativos', children=[
            html.Div([
                # Lista de Usuários Ativos
                html.Div([
                    html.H3('Usuários Ativos'),
                    dash_table.DataTable(
                        id='active-users-table',
                        columns=[
                            {'name': 'ID', 'id': 'user_id'},
                            {'name': 'Nome', 'id': 'name'},
                            {'name': 'Última Atividade', 'id': 'last_activity'},
                            {'name': 'Status', 'id': 'status'}
                        ],
                        style_table={'overflowX': 'auto'},
                        style_cell={'textAlign': 'left'},
                        style_header={
                            'backgroundColor': 'rgb(230, 230, 230)',
                            'fontWeight': 'bold'
                        },
                        row_selectable='single'
                    ),
                ], className='dashboard-card'),
                
                # Detalhes do Usuário Selecionado
                html.Div([
                    html.H3('Detalhes do Usuário'),
                    html.Div(id='user-details'),
                    dcc.Graph(id='user-interaction-graph')
                ], className='dashboard-card')
            ], style={'padding': '20px'})
        ]),
        
        # Tab 3: Memória e Contexto
        dcc.Tab(label='Memória e Contexto', children=[
            html.Div([
                # Visualização de Memória e Contexto
                html.Div([
                    html.Div([
                        html.H3('Memória e Contexto'),
                        html.Button(
                            'Limpar Contexto',
                            id='clear-context-button',
                            n_clicks=0,
                            style={
                                'marginLeft': '10px',
                                'backgroundColor': '#dc3545',
                                'color': 'white',
                                'border': 'none',
                                'padding': '5px 10px',
                                'borderRadius': '4px',
                                'cursor': 'pointer'
                            }
                        )
                    ], style={'display': 'flex', 'alignItems': 'center', 'marginBottom': '10px'}),
                    
                    # Seletor de usuário
                    dcc.Dropdown(
                        id='context-user-dropdown',
                        placeholder='Selecione um usuário'
                    ),
                    
                    # Histórico de Interações
                    html.Div([
                        html.H4('Histórico de Interações'),
                        dash_table.DataTable(
                            id='interactions-table',
                            columns=[
                                {'name': 'Timestamp', 'id': 'timestamp'},
                                {'name': 'Requisição', 'id': 'request'},
                                {'name': 'Resposta', 'id': 'response'},
                                {'name': 'Tempo (s)', 'id': 'processing_time'},
                                {'name': 'Cache Hit', 'id': 'cache_hit'},
                                {'name': 'Memórias Usadas', 'id': 'used_memories'},
                                {'name': 'Memórias de Trabalho', 'id': 'working_memories'},
                                {'name': 'Chat', 'id': 'chat_history', 'presentation': 'markdown'}
                            ],
                            style_table={
                                'overflowX': 'auto',
                                'maxHeight': '300px',
                                'overflowY': 'auto'
                            },
                            style_cell={'textAlign': 'left'},
                            style_header={
                                'backgroundColor': 'rgb(230, 230, 230)',
                                'fontWeight': 'bold'
                            },
                            style_data_conditional=[{
                                'if': {'column_id': 'content'},
                                'width': '60%',
                                'whiteSpace': 'pre-line'
                            }],
                            page_size=10,
                            sort_action='native',
                            sort_mode='multi',
                            filter_action='native',
                            tooltip_data=[{
                                'used_memories': {'value': '\n'.join(row['used_memories']) if row['used_memories'] else 'Nenhuma',
                                                'type': 'markdown'},
                                'working_memories': {'value': '\n'.join(row['working_memories']) if row['working_memories'] else 'Nenhuma',
                                                   'type': 'markdown'},
                                'chat_history': {'value': row.get('chat_history', 'Nenhum histórico'),
                                               'type': 'markdown'}
                            } for row in []]  # Será preenchido no callback
                        ),
                        
                        # Modal para visualizar histórico de chat
                        dbc.Modal(
                            [
                                dbc.ModalHeader("Histórico de Chat"),
                                dbc.ModalBody(id='chat-history-modal-body'),
                                dbc.ModalFooter(
                                    dbc.Button("Fechar", id="close-chat-history", className="ml-auto")
                                ),
                            ],
                            id="chat-history-modal",
                            size="lg",
                        ),
                    ], className='context-section'),
                    
                    # Métricas de Desempenho
                    html.Div([
                        html.H4('Métricas de Desempenho'),
                        html.Div(id='context-performance-metrics', className='metrics-container')
                    ], className='context-section'),
                    
                    # Histórico de Chat
                    html.Div([
                        html.H4('Histórico de Chat'),
                        html.Div(id='chat-history', className='chat-history')
                    ], className='context-section'),
                    
                    # Memórias de Trabalho
                    html.Div([
                        html.H4('Memórias de Trabalho'),
                        html.Div(id='working-memories', className='working-memories')
                    ], className='context-section'),
                    
                    # Sistema de Abas para Diferentes Tipos de Memória
                    dcc.Tabs([
                        dcc.Tab(label='Contexto Ativo', children=[
                            dash_table.DataTable(
                                id='context-table',
                                columns=[
                                    {'name': 'Timestamp', 'id': 'timestamp'},
                                    {'name': 'Tipo', 'id': 'type'},
                                    {'name': 'Conteúdo', 'id': 'content'}
                                ],
                                style_table={
                                    'overflowX': 'auto',
                                    'maxHeight': '300px',
                                    'overflowY': 'auto'
                                },
                                style_cell={'textAlign': 'left'},
                                style_header={
                                    'backgroundColor': 'rgb(230, 230, 230)',
                                    'fontWeight': 'bold'
                                },
                                style_data_conditional=[{
                                    'if': {'column_id': 'content'},
                                    'width': '60%',
                                    'whiteSpace': 'pre-line'
                                }],
                                page_size=10,
                                sort_action='native',
                                sort_mode='multi',
                                filter_action='native',
                                row_selectable=False,
                                selected_rows=[],
                                page_action='native'
                            )
                        ]),
                        dcc.Tab(label='Memória de Trabalho', children=[
                            dcc.Dropdown(
                                id='working-memory-user-dropdown',
                                placeholder='Selecione um usuário'
                            ),
                            dash_table.DataTable(
                                id='working-memory-table',
                                columns=[
                                    {'name': 'Timestamp', 'id': 'timestamp'},
                                    {'name': 'Conteúdo', 'id': 'content'}
                                ],
                                style_table={
                                    'overflowX': 'auto',
                                    'maxHeight': '300px',
                                    'overflowY': 'auto'
                                },
                                style_cell={'textAlign': 'left'},
                                style_header={
                                    'backgroundColor': 'rgb(230, 230, 230)',
                                    'fontWeight': 'bold'
                                },
                                style_data_conditional=[{
                                    'if': {'column_id': 'content'},
                                    'width': '60%',
                                    'whiteSpace': 'pre-line'
                                }],
                                page_size=10,
                                sort_action='native',
                                sort_mode='multi',
                                filter_action='native',
                                row_selectable=False,
                                selected_rows=[],
                                page_action='native'
                            )
                        ]),
                        dcc.Tab(label='Memória de Longo Prazo', children=[
                            dcc.Dropdown(
                                id='long-term-memory-user-dropdown',
                                placeholder='Selecione um usuário'
                            ),
                            dash_table.DataTable(
                                id='long-term-memory-table',
                                columns=[
                                    {'name': 'Timestamp', 'id': 'timestamp'},
                                    {'name': 'Conteúdo', 'id': 'content'},
                                    {'name': 'Tipo', 'id': 'type'}
                                ],
                                style_table={
                                    'overflowX': 'auto',
                                    'maxHeight': '300px',
                                    'overflowY': 'auto'
                                },
                                style_cell={'textAlign': 'left'},
                                style_header={
                                    'backgroundColor': 'rgb(230, 230, 230)',
                                    'fontWeight': 'bold'
                                },
                                style_data_conditional=[{
                                    'if': {'column_id': 'content'},
                                    'width': '60%',
                                    'whiteSpace': 'pre-line'
                                }],
                                page_size=10,
                                sort_action='native',
                                sort_mode='multi',
                                filter_action='native',
                                row_selectable=False,
                                selected_rows=[],
                                page_action='native'
                            )
                        ]),
                        dcc.Tab(label='Busca Semântica', children=[
                            dcc.Input(
                                id='memory-search-input',
                                type='text',
                                placeholder='Digite sua busca...',
                                style={'width': '100%', 'marginBottom': '10px'}
                            ),
                            dcc.Dropdown(
                                id='memory-search-user-dropdown',
                                placeholder='Selecione um usuário'
                            ),
                            dash_table.DataTable(
                                id='memory-search-table',
                                columns=[
                                    {'name': 'Timestamp', 'id': 'timestamp'},
                                    {'name': 'Conteúdo', 'id': 'content'},
                                    {'name': 'Similaridade', 'id': 'similarity'}
                                ],
                                style_table={
                                    'overflowX': 'auto',
                                    'maxHeight': '300px',
                                    'overflowY': 'auto'
                                },
                                style_cell={'textAlign': 'left'},
                                style_header={
                                    'backgroundColor': 'rgb(230, 230, 230)',
                                    'fontWeight': 'bold'
                                },
                                style_data_conditional=[{
                                    'if': {'column_id': 'content'},
                                    'width': '60%',
                                    'whiteSpace': 'pre-line'
                                }],
                                page_size=10,
                                sort_action='native',
                                sort_mode='multi',
                                filter_action='native',
                                row_selectable=False,
                                selected_rows=[],
                                page_action='native'
                            )
                        ])
                    ])
                ], className='dashboard-card'),
                
                # Informações do Sistema
                html.Div([
                    html.H3('Informações do Sistema'),
                    html.Div(id='system-info', style={'marginTop': '10px'})
                ], className='dashboard-card'),
                
                # Métricas de Uso de Memória
                html.Div([
                    html.H3('Métricas de Uso de Memória'),
                    dcc.Graph(id='memory-usage-graph')
                ], className='dashboard-card')
            ], style={'padding': '20px'})
        ]),        
        # Tab 4: API e Cache
        dcc.Tab(label='API e Cache', children=[
            html.Div([
                # Métricas da API
                html.Div([
                    html.H3('Métricas da API'),
                    dcc.Graph(id='api-metrics-graph'),
                    html.Div(id='api-stats')
                ], className='dashboard-card'),
                
                # Métricas de Cache
                html.Div([
                    html.H3('Métricas de Cache'),
                    html.Div([
                        html.Div(id='cache-stats'),
                        dcc.Graph(id='cache-hit-ratio-graph')
                    ])
                ], className='dashboard-card'),
                
                # Log de Operações
                html.Div([
                    html.H3('Log de Operações'),
                    dash_table.DataTable(
                        id='operations-log-table',
                        columns=[
                            {'name': 'Timestamp', 'id': 'timestamp'},
                            {'name': 'Operação', 'id': 'operation'},
                            {'name': 'Status', 'id': 'status'},
                            {'name': 'Detalhes', 'id': 'details'}
                        ],
                        style_table={'overflowX': 'auto'},
                        style_cell={'textAlign': 'left'},
                        style_data_conditional=[{
                            'if': {'column_id': 'details'},
                            'whiteSpace': 'normal',
                            'height': 'auto',
                        }]
                    )
                ], className='dashboard-card'),

                # Contexto Ativo
                html.Div([
                    html.H3('Contexto Ativo'),
                    dash_table.DataTable(
                        id='context-table',
                        columns=[
                            {'name': 'Timestamp', 'id': 'timestamp'},
                            {'name': 'Tipo', 'id': 'type'},
                            {'name': 'Conteúdo', 'id': 'content'}
                        ],
                        style_table={'overflowX': 'auto'},
                        style_cell={
                            'textAlign': 'left',
                            'whiteSpace': 'normal',
                            'height': 'auto',
                            'minWidth': '100px',
                            'maxWidth': '500px'
                        },
                        style_header={
                            'backgroundColor': 'rgb(230, 230, 230)',
                            'fontWeight': 'bold',
                            'textAlign': 'center'
                        },
                        style_data_conditional=[{
                            'if': {'column_id': 'content'},
                            'width': '60%',
                            'whiteSpace': 'pre-line'
                        }],
                        page_size=10,
                        sort_action='native',
                        sort_mode='multi'
                    )
                ], className='dashboard-card')
            ], style={'padding': '20px'})
        ])
    ]),
    
    # Intervals for updates
    dcc.Interval(
        id='active-users-interval',
        interval=5000,  # 5 segundos
        n_intervals=0
    ),
    dcc.Interval(
        id='memory-operations-interval',
        interval=5000,  # 5 segundos
        n_intervals=0
    ),
    dcc.Interval(
        id='api-metrics-interval',
        interval=5000,  # 5 segundos
        n_intervals=0
    ),
    dcc.Interval(id='message-processing-interval', interval=5000, n_intervals=0),
    dcc.Interval(id='resource-usage-interval', interval=5000, n_intervals=0),
    dcc.Interval(id='context-interval', interval=3000, n_intervals=0)
])

# Callbacks
@app.callback(
    Output('bot-status', 'children'),
    Input('message-processing-interval', 'n_intervals')
)
def update_bot_status(n):
    """Update bot status display."""
    try:
        response = requests.get(f"{API_BASE_URL}/status")
        data = response.json()
        status = data['status']
        
        # Define color based on status
        status_colors = {
            'running': 'green',
            'stopped': 'red',
            'paused': 'orange',
            'initialized': 'blue',
            'error': 'red',
            'unknown': 'grey'
        }
        color = status_colors.get(status['status'].lower(), 'grey')
        
        return html.Div([
            html.H3(f"Status: {status['status'].upper()}",
                   style={'color': color}),
            html.P(f"Last Updated: {status['timestamp'] or 'Never'}")
        ])
    except Exception as e:
        return html.Div([
            html.H3("Status: DISCONNECTED",
                   style={'color': 'red'}),
            html.P(f"Error: {str(e)}", style={'color': 'red'})
        ])

@app.callback(
    Output('message-processing-graph', 'figure'),
    Input('message-processing-interval', 'n_intervals')
)
def update_message_processing(n):
    """Update message processing graph."""
    try:
        response = requests.get(f"{API_BASE_URL}/metrics/messages")
        data = response.json()
        
        if not data:  # Empty data
            return create_empty_graph("No message processing data available")
            
        df = pd.DataFrame(data)
        if df.empty:
            return create_empty_graph("No message processing data available")
            
        # Create time series plot
        fig = go.Figure()
        
        for msg_type in df['message_type'].unique():
            type_data = df[df['message_type'] == msg_type]
            fig.add_trace(go.Scatter(
                x=type_data['timestamp'],
                y=type_data['processing_time'],
                name=msg_type,
                mode='lines+markers'
            ))
            
        fig.update_layout(
            title='Message Processing Time by Type',
            xaxis_title='Time',
            yaxis_title='Processing Time (s)',
            template='plotly_white',
            showlegend=True
        )
        
        return fig
    except Exception as e:
        return create_empty_graph(f"Error loading message data: {str(e)}")

@app.callback(
    Output('resource-usage-graph', 'figure'),
    Input('resource-usage-interval', 'n_intervals')
)
def update_resource_usage(n):
    """Update resource usage graph."""
    try:
        response = requests.get(f"{API_BASE_URL}/metrics/resources")
        data = response.json()
        
        if not data:  # Empty data
            return create_empty_graph("No resource usage data available")
            
        df = pd.DataFrame(data)
        if df.empty:
            return create_empty_graph("No resource usage data available")
            
        fig = go.Figure()
        
        for resource in df['resource_type'].unique():
            resource_data = df[df['resource_type'] == resource]
            fig.add_trace(go.Scatter(
                x=resource_data['timestamp'],
                y=resource_data['usage_value'],
                name=resource,
                mode='lines'
            ))
        
        fig.update_layout(
            title='Resource Usage Over Time',
            xaxis_title='Time',
            yaxis_title='Usage Value',
            template='plotly_white',
            showlegend=True
        )
        
        return fig
    except Exception as e:
        return create_empty_graph(f"Error loading resource usage data: {str(e)}")

@app.callback(
    Output('active-users-table', 'data'),
    Input('active-users-interval', 'n_intervals')
)
def update_active_users(n):
    """Update active users table."""
    try:
        response = requests.get(f"{API_BASE_URL}/users/active")
        data = response.json()
        
        if not data:  # Empty data
            return []
            
        df = pd.DataFrame(data)
        if df.empty:
            return []
            
        return df.to_dict('records')
    except Exception as e:
        return []

@app.callback(
    Output('user-details', 'children'),
    [Input('active-users-table', 'selected_rows'),
     Input('active-users-table', 'data')]
)
def update_user_details(selected_rows, data):
    """Update user details."""
    if not selected_rows:
        return ""
    
    user_id = data[selected_rows[0]]['user_id']
    
    try:
        response = requests.get(f"{API_BASE_URL}/users/{user_id}")
        data = response.json()
        
        return html.Div([
            html.P(f"Nome: {data['name']}"),
            html.P(f"Última Atividade: {data['last_activity']}"),
            html.P(f"Status: {data['status']}")
        ])
    except Exception as e:
        return f"Error loading user data: {str(e)}"

@app.callback(
    Output('user-interaction-graph', 'figure'),
    [Input('active-users-table', 'selected_rows'),
     Input('active-users-table', 'data')]
)
def update_user_interaction_graph(selected_rows, data):
    """Update user interaction graph."""
    if not selected_rows:
        return create_empty_graph("No user selected")
    
    user_id = data[selected_rows[0]]['user_id']
    
    try:
        response = requests.get(f"{API_BASE_URL}/users/{user_id}/interactions")
        data = response.json()
        
        if not data:  # Empty data
            return create_empty_graph("No user interaction data available")
            
        df = pd.DataFrame(data)
        if df.empty:
            return create_empty_graph("No user interaction data available")
            
        fig = go.Figure()
        
        fig.add_trace(go.Scatter(
            x=df['timestamp'],
            y=df['interaction_type'],
            mode='markers'
        ))
        
        fig.update_layout(
            title='User Interaction Over Time',
            xaxis_title='Time',
            yaxis_title='Interaction Type',
            template='plotly_white',
            showlegend=False
        )
        
        return fig
    except Exception as e:
        return create_empty_graph(f"Error loading user interaction data: {str(e)}")

@app.callback(
    [Output('memory-stats', 'children'),
     Output('memory-operations-graph', 'figure'),
     Output('memory-distribution-graph', 'figure')],
    Input('memory-operations-interval', 'n_intervals')
)
def update_memory_metrics(n):
    """Update memory metrics including stats, operations and distribution."""
    stats_output = html.P("Erro ao carregar estatísticas de memória")
    ops_output = create_empty_graph("Sem dados de operações")
    dist_output = create_empty_graph("Sem dados de distribuição")
    
    try:
        # Busca estatísticas
        response = requests.get(f"{API_BASE_URL}/metrics/memory")
        if response.status_code == 200:
            data = response.json()
            if data and len(data) > 0:
                # Pega o último registro para as estatísticas
                latest = data[-1]
                stats_output = html.Div([
                    html.P(f"Total de Operações: {latest.get('total_operations', 0)}"),
                    html.P(f"Latência Média: {latest.get('avg_latency', 0):.2f}ms"),
                    html.P(f"Taxa de Cache Hit: {latest.get('cache_hit_rate', 0):.1f}%"),
                    html.P(f"Tempo Médio de Embedding: {latest.get('avg_embedding_time', 0):.2f}ms")
                ])
                
                # Prepara dados para o gráfico de operações
                timestamps = [record.get('time_bucket', '') for record in data]
                operations = [record.get('total_operations', 0) for record in data]
                latencies = [record.get('avg_latency', 0) for record in data]
                
                ops_output = {
                    'data': [
                        go.Scatter(x=timestamps, y=operations, 
                                 name='Total de Operações', mode='lines+markers'),
                        go.Scatter(x=timestamps, y=latencies, 
                                 name='Latência Média (ms)', mode='lines+markers',
                                 yaxis='y2')
                    ],
                    'layout': {
                        'title': 'Operações de Memória',
                        'xaxis': {'title': 'Tempo'},
                        'yaxis': {'title': 'Total de Operações'},
                        'yaxis2': {
                            'title': 'Latência (ms)',
                            'overlaying': 'y',
                            'side': 'right'
                        },
                        'height': 400,
                        'showlegend': True
                    }
                }
                
                # Prepara dados para o gráfico de distribuição
                dist_data = {
                    'Cache Hits': sum(1 for r in data if r.get('cache_hit_rate', 0) > 50),
                    'Cache Misses': sum(1 for r in data if r.get('cache_hit_rate', 0) <= 50)
                }
                
                dist_output = {
                    'data': [go.Pie(labels=list(dist_data.keys()), values=list(dist_data.values()))],
                    'layout': {
                        'title': 'Distribuição de Cache Hits/Misses',
                        'height': 400
                    }
                }
                
    except Exception as e:
        print(f"Error updating memory metrics: {e}")
    
    return stats_output, ops_output, dist_output

@app.callback(
    [Output('context-performance-metrics', 'children'),
     Output('chat-history', 'children'),
     Output('working-memories', 'children')],
    [Input('context-user-dropdown', 'value'),
     Input('memory-operations-interval', 'n_intervals')]
)
def update_context_info(user_id, n):
    """Update context information including performance metrics, chat history and working memories."""
    if not user_id:
        return html.P("Selecione um usuário"), html.P("Selecione um usuário"), html.P("Selecione um usuário")
    
    try:
        response = requests.get(f"{API_BASE_URL}/metrics/context/{user_id}")
        if response.status_code == 200:
            data = response.json()
            
            # Performance Metrics
            performance = data['performance_metrics']
            metrics_div = html.Div([
                html.P(f"Total de Operações: {performance['total_operations']}"),
                html.P(f"Cache Hits: {performance['cache_hits']}"),
                html.P(f"Taxa de Cache Hit: {performance['cache_hit_rate']:.1f}%"),
                html.P(f"Latência Média: {performance['avg_latency']:.2f}ms")
            ])
            
            # Chat History
            chat_history = data['chat_history'].split('\n')
            chat_div = html.Div([
                html.P(line) for line in chat_history if line.strip()
            ], style={'maxHeight': '400px', 'overflowY': 'auto'})
            
            # Working Memories
            memories_div = html.Div([
                html.P(memory) for memory in data['working_memories']
            ], style={'maxHeight': '400px', 'overflowY': 'auto'})
            
            return metrics_div, chat_div, memories_div
            
    except Exception as e:
        print(f"Error updating context info: {e}")
        return (
            html.P("Erro ao carregar métricas"),
            html.P("Erro ao carregar histórico"),
            html.P("Erro ao carregar memórias")
        )

@app.callback(
    [Output('interactions-table', 'data'),
     Output('interactions-table', 'tooltip_data')],
    [Input('context-user-dropdown', 'value'),
     Input('memory-operations-interval', 'n_intervals')]
)
def update_interactions_table(user_id, n):
    """Update interactions table with request/response history."""
    if not user_id:
        return [], []
    
    try:
        response = requests.get(f"{API_BASE_URL}/metrics/interactions/{user_id}")
        if response.status_code == 200:
            interactions = response.json()
            
            # Prepara dados da tabela
            table_data = [{
                'timestamp': interaction['timestamp'],
                'request': interaction['request'],
                'response': interaction['response'],
                'processing_time': f"{interaction['processing_time']:.2f}",
                'cache_hit': '✓' if interaction['cache_hit'] else '✗',
                'used_memories': f"{len(interaction['used_memories'])} memórias",
                'working_memories': f"{len(interaction['working_memories'])} memórias",
                'chat_history': '[Ver histórico]'
            } for interaction in interactions]
            
            # Prepara tooltips
            tooltip_data = [{
                'used_memories': {
                    'value': '\n'.join(interaction['used_memories']) if interaction['used_memories'] else 'Nenhuma',
                    'type': 'markdown'
                },
                'working_memories': {
                    'value': '\n'.join(interaction['working_memories']) if interaction['working_memories'] else 'Nenhuma',
                    'type': 'markdown'
                },
                'chat_history': {
                    'value': interaction['chat_history'] if interaction['chat_history'] else 'Nenhum histórico',
                    'type': 'markdown'
                }
            } for interaction in interactions]
            
            return table_data, tooltip_data
            
    except Exception as e:
        print(f"Error updating interactions table: {e}")
    return [], []

@app.callback(
    Output('memory-search-table', 'data'),
    [Input('memory-search-input', 'value'),
     Input('memory-search-user-dropdown', 'value')]
)
def update_memory_search_table(query, user_id):
    """Update memory search table."""
    if not query or not user_id:
        return []
    
    try:
        response = requests.get(
            f"{API_BASE_URL}/metrics/similar_memories",
            params={'query': query, 'user_id': user_id}
        )
        if response.status_code == 200:
            memories = response.json()
            return [{
                'timestamp': memory['metadata']['timestamp'],
                'content': memory['content'],
                'similarity': f"{memory['similarity']:.2f}"
            } for memory in memories]
            
    except Exception as e:
        print(f"Error updating memory search: {e}")
    return []

@app.callback(
    Output('working-memory-table', 'data'),
    [Input('working-memory-user-dropdown', 'value'),
     Input('memory-operations-interval', 'n_intervals')]
)
def update_working_memory_table(user_id, n):
    """Update working memory table."""
    if not user_id:
        return []
    
    try:
        response = requests.get(f"{API_BASE_URL}/metrics/working_memory/{user_id}")
        if response.status_code == 200:
            memories = response.json()
            return [{
                'timestamp': memory['timestamp'],
                'content': memory['content']
            } for memory in memories]
            
    except Exception as e:
        print(f"Error updating working memory table: {e}")
    return []

@app.callback(
    Output('long-term-memory-table', 'data'),
    [Input('long-term-memory-user-dropdown', 'value'),
     Input('memory-operations-interval', 'n_intervals')]
)
def update_long_term_memory_table(user_id, n):
    """Update long-term memory table."""
    if not user_id:
        return []
    
    try:
        response = requests.get(f"{API_BASE_URL}/metrics/long_term_memory/{user_id}")
        if response.status_code == 200:
            memories = response.json()
            return [{
                'timestamp': memory['metadata']['timestamp'],
                'content': memory['content'],
                'type': memory['metadata']['type']
            } for memory in memories]
            
    except Exception as e:
        print(f"Error updating long-term memory table: {e}")
    return []

@app.callback(
    Output('context-table', 'data'),
    Input('context-interval', 'n_intervals')
)
def update_context_table(n):
    """Update context table."""
    try:
        response = requests.get(f"{API_BASE_URL}/context")
        data = response.json()
        
        if not data:  # Empty data
            return []
            
        # Formata os dados para exibição
        formatted_data = []
        for item in data:
            # Formata o timestamp para exibição mais amigável
            timestamp = datetime.strptime(item['timestamp'], '%Y-%m-%d %H:%M:%S')
            formatted_timestamp = timestamp.strftime('%d/%m/%Y %H:%M')
            
            # Formata o tipo para exibição mais amigável
            type_display = {
                'user_message': 'Mensagem do Usuário',
                'bot_message': 'Resposta do Bot',
                'document': 'Documento',
                'memory': 'Memória'
            }.get(item['type'], item['type'])
            
            formatted_data.append({
                'timestamp': formatted_timestamp,
                'type': type_display,
                'content': item['content']
            })
            
        # Ordena por timestamp decrescente
        formatted_data.sort(key=lambda x: datetime.strptime(x['timestamp'], '%d/%m/%Y %H:%M'), reverse=True)
        
        return formatted_data
    except Exception as e:
        print(f"Error updating context table: {e}")
        return []

@app.callback(
    Output('api-metrics-graph', 'figure'),
    Input('api-metrics-interval', 'n_intervals')
)
def update_api_metrics_graph(n):
    """Update API metrics graph."""
    try:
        response = requests.get(f"{API_BASE_URL}/metrics/api")
        if response.status_code == 200:
            data = response.json()
            if not data or 'metrics' not in data:
                return {}
            
            metrics = data['metrics']
            timestamps = []
            requests_per_min = []
            response_times = []
            
            for metric in metrics:
                timestamps.append(metric.get('timestamp', ''))
                requests_per_min.append(metric.get('requests_per_minute', 0))
                response_times.append(metric.get('response_time', 0))
            
            # Cria gráfico com subplots
            fig = make_subplots(rows=2, cols=1,
                              subplot_titles=('Requisições por Minuto',
                                            'Tempo de Resposta (ms)'))
            
            # Adiciona traces
            fig.add_trace(
                go.Scatter(x=timestamps, 
                         y=requests_per_min,
                         name='Requisições/min',
                         mode='lines+markers'),
                row=1, col=1
            )
            
            fig.add_trace(
                go.Scatter(x=timestamps, 
                         y=response_times,
                         name='Tempo de Resposta',
                         mode='lines+markers'),
                row=2, col=1
            )
            
            # Atualiza layout
            fig.update_layout(height=600, showlegend=True)
            
            return fig
    except Exception as e:
        print(f"Error updating API metrics graph: {e}")
        return {}

@app.callback(
    Output('cache-stats', 'children'),
    Input('memory-operations-interval', 'n_intervals')
)
def update_cache_stats(n):
    """Update cache stats."""
    try:
        response = requests.get(f"{API_BASE_URL}/metrics/cache")
        if response.status_code == 200:
            data = response.json()
            if not data or 'stats' not in data:
                return html.P("Sem dados de cache disponíveis")
            
            stats = data['stats']
            return html.Div([
                html.P(f"Total Cache Hits: {stats.get('cache_hits', 0)}"),
                html.P(f"Total Cache Misses: {stats.get('cache_misses', 0)}"),
                html.P(f"Cache Hit Ratio: {stats.get('cache_hit_ratio', 0):.2f}%")
            ])
    except Exception as e:
        print(f"Error updating cache stats: {e}")
        return html.P("Erro ao carregar estatísticas de cache")

@app.callback(
    Output('cache-hit-ratio-graph', 'figure'),
    Input('memory-operations-interval', 'n_intervals')
)
def update_cache_hit_ratio_graph(n):
    """Update cache hit ratio graph."""
    try:
        response = requests.get(f"{API_BASE_URL}/metrics/cache")
        if response.status_code == 200:
            data = response.json()
            if not data or 'metrics' not in data:
                return {}
            
            metrics = data['metrics']
            timestamps = []
            hit_ratios = []
            
            for metric in metrics:
                timestamps.append(metric.get('timestamp', ''))
                hit_ratios.append(metric.get('cache_hit_ratio', 0))
            
            # Cria gráfico
            fig = go.Figure()
            
            fig.add_trace(go.Scatter(
                x=timestamps,
                y=hit_ratios,
                mode='lines+markers',
                name='Taxa de Cache Hit'
            ))
            
            # Atualiza layout
            fig.update_layout(
                title='Taxa de Cache Hit',
                xaxis_title='Tempo',
                yaxis_title='Taxa de Cache Hit (%)',
                height=400
            )
            
            return fig
    except Exception as e:
        print(f"Error updating cache hit ratio graph: {e}")
        return {}

@app.callback(
    Output('operations-log-table', 'data'),
    Input('memory-operations-interval', 'n_intervals')
)
def update_operations_log_table(n):
    """Update operations log table."""
    try:
        response = requests.get(f"{API_BASE_URL}/log/operations")
        if response.status_code == 200:
            data = response.json()
            if not data or 'log' not in data:
                return []
            
            log_entries = []
            for entry in data['log']:
                log_entries.append({
                    'timestamp': entry.get('timestamp', ''),
                    'operation': entry.get('operation', ''),
                    'status': entry.get('status', ''),
                    'details': entry.get('details', '')
                })
            return log_entries
    except Exception as e:
        print(f"Error updating operations log table: {e}")
        return []

@app.callback(
    Output('control-status', 'children'),
    [Input('start-button', 'n_clicks'),
     Input('stop-button', 'n_clicks'),
     Input('pause-button', 'n_clicks')],
    [State('control-status', 'children')]
)
def handle_control_buttons(start_clicks, stop_clicks, pause_clicks, current_status):
    """Handle bot control button clicks."""
    ctx = dash.callback_context
    if not ctx.triggered:
        return current_status or ""
    
    button_id = ctx.triggered[0]['prop_id'].split('.')[0]
    command = None
    
    if button_id == 'start-button':
        command = 'start'
    elif button_id == 'stop-button':
        command = 'stop'
    elif button_id == 'pause-button':
        command = 'pause'
    
    if command:
        try:
            response = requests.post(
                f"{API_BASE_URL}/control",
                json={"command": command, "reason": f"Command triggered from dashboard"}
            )
            data = response.json()
            return f"Command '{command}' executed successfully" if data['success'] else f"Failed to execute command '{command}'"
        except Exception as e:
            return f"Error executing command: {str(e)}"
    
    return current_status or ""

@app.callback(
    Output('clear-context-button', 'n_clicks'),
    Input('clear-context-button', 'n_clicks'),
    prevent_initial_call=True
)
def clear_context(n_clicks):
    """Clear context when button is clicked."""
    if n_clicks:
        try:
            response = requests.post(f"{API_BASE_URL}/context/clear")
            if response.status_code == 200:
                print("Context cleared successfully")
            else:
                print(f"Error clearing context: {response.text}")
        except Exception as e:
            print(f"Error clearing context: {e}")
    return 0  # Reset click count

@app.callback(
    [Output('working-memory-user-dropdown', 'options'),
     Output('long-term-memory-user-dropdown', 'options'),
     Output('memory-search-user-dropdown', 'options')],
    Input('active-users-interval', 'n_intervals')
)
def update_user_dropdowns(n):
    """Update user dropdowns."""
    try:
        response = requests.get(f"{API_BASE_URL}/users/active")
        if response.status_code == 200:
            users = response.json()
            options = [{'label': f"{user['first_name']} ({user['id']})", 'value': user['id']} 
                      for user in users]
            return options, options, options
    except Exception as e:
        print(f"Error updating user dropdowns: {e}")
    return [], [], []

@app.callback(
    Output('system-info', 'children'),
    Input('memory-operations-interval', 'n_intervals')
)
def update_system_info(n):
    """Update system information."""
    try:
        response = requests.get(f"{API_BASE_URL}/system/info")
        if response.status_code == 200:
            info = response.json()
            return html.Div([
                html.P(f"Modelo: {info['model']}"),
                html.P(f"Limite de Histórico: {info['max_history']} mensagens"),
                html.P(f"Limite de Memória de Trabalho: {info['max_working_memory']} itens"),
                html.P(f"Intervalo de Limpeza: {info['cleanup_interval']}"),
                html.H4("Capacidades:", style={'marginTop': '10px'}),
                html.Ul([
                    html.Li("Processamento de Texto ✓" if info['capabilities']['text_processing'] else "Processamento de Texto ✗"),
                    html.Li("Processamento de Imagem ✓" if info['capabilities']['image_processing'] else "Processamento de Imagem ✗"),
                    html.Li("Processamento de Áudio ✓" if info['capabilities']['audio_processing'] else "Processamento de Áudio ✗"),
                    html.Li("Gerenciamento de Memória ✓" if info['capabilities']['memory_management'] else "Gerenciamento de Memória ✗")
                ])
            ])
    except Exception as e:
        print(f"Error updating system info: {e}")
        return html.P("Erro ao carregar informações do sistema")

@app.callback(
    Output('memory-usage-graph', 'figure'),
    Input('memory-operations-interval', 'n_intervals')
)
def update_memory_usage_graph(n):
    """Update memory usage graph."""
    try:
        response = requests.get(f"{API_BASE_URL}/metrics/memory_usage")
        if response.status_code == 200:
            data = response.json()
            if not data or 'metrics' not in data:
                return {}
            
            metrics = data['metrics']
            timestamps = []
            total_operations = []
            avg_latency = []
            success_rate = []
            
            for metric in metrics:
                timestamps.append(metric.get('timestamp', ''))
                total_operations.append(metric.get('total_operations', 0))
                avg_latency.append(metric.get('average_latency', 0))
                success_rate.append(metric.get('success_rate', 0))
            
            # Cria gráfico com subplots
            fig = make_subplots(rows=3, cols=1,
                              subplot_titles=('Total de Operações',
                                            'Latência Média (s)',
                                            'Taxa de Sucesso (%)'))
            
            # Adiciona traces para cada métrica
            fig.add_trace(
                go.Scatter(x=timestamps, 
                         y=total_operations,
                         name='Total de Operações',
                         mode='lines+markers'),
                row=1, col=1
            )
            
            fig.add_trace(
                go.Scatter(x=timestamps, 
                         y=avg_latency,
                         name='Latência Média',
                         mode='lines+markers'),
                row=2, col=1
            )
            
            fig.add_trace(
                go.Scatter(x=timestamps, 
                         y=success_rate,
                         name='Taxa de Sucesso',
                         mode='lines+markers'),
                row=3, col=1
            )
            
            # Atualiza layout
            fig.update_layout(height=800, showlegend=True)
            
            return fig
    except Exception as e:
        print(f"Error updating memory usage graph: {e}")
        return {}

@app.callback(
    [Output("chat-history-modal", "is_open"),
     Output("chat-history-modal-body", "children")],
    [Input('interactions-table', 'active_cell'),
     Input("close-chat-history", "n_clicks")],
    [State('interactions-table', 'data'),
     State("chat-history-modal", "is_open")]
)
def toggle_chat_history_modal(active_cell, close_clicks, table_data, is_open):
    """Toggle chat history modal and update content."""
    ctx = dash.callback_context
    if not ctx.triggered:
        return False, ""
    
    trigger_id = ctx.triggered[0]['prop_id'].split('.')[0]
    
    if trigger_id == "close-chat-history":
        return False, ""
    
    if active_cell and active_cell['column_id'] == 'chat_history':
        row = table_data[active_cell['row']]
        
        # Busca histórico completo do chat
        try:
            response = requests.get(
                f"{API_BASE_URL}/metrics/interactions/{row['user_id']}/chat_history",
                params={'timestamp': row['timestamp']}
            )
            if response.status_code == 200:
                chat_history = response.json()['chat_history']
                return True, html.Div([
                    html.P(message) for message in chat_history.split('\n') if message.strip()
                ])
        except Exception as e:
            print(f"Error fetching chat history: {e}")
            return True, "Erro ao carregar histórico"
    
    return False, ""

def create_empty_graph(message: str):
    """Create an empty graph with a message."""
    return {
        'data': [],
        'layout': {
            'title': message,
            'xaxis': {'visible': False},
            'yaxis': {'visible': False},
            'annotations': [{
                'text': message,
                'xref': 'paper',
                'yref': 'paper',
                'showarrow': False,
                'font': {'size': 14}
            }]
        }
    }

if __name__ == '__main__':
    app.run_server(debug=True, port=8050)
