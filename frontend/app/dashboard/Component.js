import React from "react";


export const Component = () => {
  return (
<div id="webcrumbs"> 
        	<div className="w-[1200px] bg-gray-50 p-6 font-sans">
	  <header className="mb-6">
	    <h1 className="text-3xl font-bold text-gray-800">Document Management Dashboard</h1>
	    <div className="mt-2 flex items-center gap-2">
	      <span className="rounded-md bg-blue-100 px-3 py-1 text-sm font-medium text-blue-800">RAG Workflow</span>
	      <div className="ml-auto flex gap-3">
	        <button className="flex items-center gap-2 rounded-md border border-gray-300 bg-white px-4 py-2 font-medium text-gray-700 shadow-sm transition-all hover:bg-gray-50 hover:shadow">
	          <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-gray-500">
	            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
	            <polyline points="7 10 12 15 17 10"></polyline>
	            <line x1="12" y1="15" x2="12" y2="3"></line>
	          </svg>
	          Import Documents
	        </button>
	        <div className="relative">
	  <button 
	    className="flex items-center gap-2 rounded-md bg-blue-600 px-4 py-2 font-medium text-white shadow-sm transition-all hover:bg-blue-700 hover:shadow-md hover:-translate-y-0.5"
	    onClick={() => {
	      document.getElementById('documentModal').classList.remove('hidden');
	    }}
	  >
	    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="transition-transform group-hover:rotate-90">
	      <line x1="12" y1="5" x2="12" y2="19"></line>
	      <line x1="5" y1="12" x2="19" y2="12"></line>
	    </svg>
	    Add Document
	  </button>
	  
	  <div id="documentModal" className="hidden absolute top-12 right-0 w-[350px] bg-white rounded-lg shadow-xl border border-gray-200 p-4 z-10 transform transition-all duration-300 ease-in-out">
	    <div className="flex justify-between items-center mb-4">
	      <h3 className="text-lg font-semibold">Select Document Type</h3>
	      <button 
	        className="text-gray-500 hover:text-gray-700 transition-colors" 
	        onClick={() => {
	          document.getElementById('documentModal').classList.add('hidden');
	        }}
	      >
	        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
	          <line x1="18" y1="6" x2="6" y2="18"></line>
	          <line x1="6" y1="6" x2="18" y2="18"></line>
	        </svg>
	      </button>
	    </div>
	    
	    <div className="grid grid-cols-2 gap-3">
	      <div className="p-3 border border-gray-200 rounded-md hover:bg-gray-50 cursor-pointer transition-all hover:shadow-md hover:-translate-y-0.5 group">
	        <div className="flex flex-col items-center">
	          <span className="text-blue-500 mb-2">
	            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
	              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
	              <polyline points="14 2 14 8 20 8"></polyline>
	              <line x1="16" y1="13" x2="8" y2="13"></line>
	              <line x1="16" y1="17" x2="8" y2="17"></line>
	              <polyline points="10 9 9 9 8 9"></polyline>
	            </svg>
	          </span>
	          <span className="font-medium">PDF Document</span>
	        </div>
	      </div>
	      
	      <div className="p-3 border border-gray-200 rounded-md hover:bg-gray-50 cursor-pointer transition-all hover:shadow-md hover:-translate-y-0.5 group">
	        <div className="flex flex-col items-center">
	          <span className="text-green-500 mb-2">
	            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
	              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
	              <polyline points="14 2 14 8 20 8"></polyline>
	              <line x1="16" y1="13" x2="8" y2="13"></line>
	              <line x1="16" y1="17" x2="8" y2="17"></line>
	              <polyline points="10 9 9 9 8 9"></polyline>
	            </svg>
	          </span>
	          <span className="font-medium">Excel File</span>
	        </div>
	      </div>
	      
	      <div className="p-3 border border-gray-200 rounded-md hover:bg-gray-50 cursor-pointer transition-all hover:shadow-md hover:-translate-y-0.5 group">
	        <div className="flex flex-col items-center">
	          <span className="text-indigo-500 mb-2">
	            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
	              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
	              <polyline points="14 2 14 8 20 8"></polyline>
	              <line x1="16" y1="13" x2="8" y2="13"></line>
	              <line x1="16" y1="17" x2="8" y2="17"></line>
	              <polyline points="10 9 9 9 8 9"></polyline>
	            </svg>
	          </span>
	          <span className="font-medium">Word Document</span>
	        </div>
	      </div>
	      
	      <div className="p-3 border border-gray-200 rounded-md hover:bg-gray-50 cursor-pointer transition-all hover:shadow-md hover:-translate-y-0.5 group">
	        <div className="flex flex-col items-center">
	          <span className="text-yellow-500 mb-2">
	            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
	              <path d="M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z"></path>
	              <polyline points="13 2 13 9 20 9"></polyline>
	            </svg>
	          </span>
	          <span className="font-medium">Other File</span>
	        </div>
	      </div>
	    </div>
	    
	    <div className="mt-4 flex justify-between items-center">
	      <span className="text-sm text-gray-500">Or drag and drop files here</span>
	      <button className="px-3 py-1.5 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors">
	        Browse Files
	      </button>
	    </div>
	  </div>
	</div>
	      </div>
	    </div>
	  </header>
	
	  <div className="mb-6 grid grid-cols-4 gap-4">
	    <div className="rounded-lg bg-white p-4 shadow transition-all hover:shadow-md">
	      <div className="flex items-center">
	        <div className="mr-4 flex h-12 w-12 items-center justify-center rounded-full bg-blue-100">
	          <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-blue-600">
	            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
	            <polyline points="14 2 14 8 20 8"></polyline>
	            <line x1="16" y1="13" x2="8" y2="13"></line>
	            <line x1="16" y1="17" x2="8" y2="17"></line>
	            <polyline points="10 9 9 9 8 9"></polyline>
	          </svg>
	        </div>
	        <div>
	          <p className="text-sm text-gray-500">Total Documents</p>
	          <h3 className="text-2xl font-bold">128</h3>
	        </div>
	      </div>
	    </div>
	    
	    <div className="rounded-lg bg-white p-4 shadow transition-all hover:shadow-md">
	      <div className="flex items-center">
	        <div className="mr-4 flex h-12 w-12 items-center justify-center rounded-full bg-green-100">
	          <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-green-600">
	            <rect x="2" y="7" width="20" height="14" rx="2" ry="2"></rect>
	            <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"></path>
	          </svg>
	        </div>
	        <div>
	          <p className="text-sm text-gray-500">Total Chunks</p>
	          <h3 className="text-2xl font-bold">2,354</h3>
	        </div>
	      </div>
	    </div>
	    
	    <div className="rounded-lg bg-white p-4 shadow transition-all hover:shadow-md">
	      <div className="flex items-center">
	        <div className="mr-4 flex h-12 w-12 items-center justify-center rounded-full bg-yellow-100">
	          <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-yellow-600">
	            <path d="M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z"></path>
	            <polyline points="13 2 13 9 20 9"></polyline>
	          </svg>
	        </div>
	        <div>
	          <p className="text-sm text-gray-500">Avg. Chunks per Doc</p>
	          <h3 className="text-2xl font-bold">18.4</h3>
	        </div>
	      </div>
	    </div>
	    
	    <div className="rounded-lg bg-white p-4 shadow transition-all hover:shadow-md">
	      <div className="flex items-center">
	        <div className="mr-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
	          <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-red-600">
	            <circle cx="12" cy="12" r="10"></circle>
	            <line x1="12" y1="8" x2="12" y2="12"></line>
	            <line x1="12" y1="16" x2="12.01" y2="16"></line>
	          </svg>
	        </div>
	        <div>
	          <p className="text-sm text-gray-500">Processing Errors</p>
	          <h3 className="text-2xl font-bold">7</h3>
	        </div>
	      </div>
	    </div>
	  </div>
	
	  <div className="mb-6 rounded-lg bg-white p-4 shadow">
	    <div className="mb-3 flex items-center justify-between">
	      <h2 className="text-lg font-semibold text-gray-800">Document Processing Trends</h2>
	      <div className="flex items-center gap-2">
	        <div className="flex items-center gap-1">
	          <div className="h-3 w-3 rounded-full bg-blue-500"></div>
	          <span className="text-xs text-gray-600">Processed</span>
	        </div>
	        <div className="flex items-center gap-1">
	          <div className="h-3 w-3 rounded-full bg-yellow-500"></div>
	          <span className="text-xs text-gray-600">Pending</span>
	        </div>
	        <div className="flex items-center gap-1">
	          <div className="h-3 w-3 rounded-full bg-red-500"></div>
	          <span className="text-xs text-gray-600">Errors</span>
	        </div>
	        <select className="ml-4 rounded-md border border-gray-300 bg-white px-2 py-1 text-sm text-gray-700 shadow-sm transition-all hover:border-blue-300">
	          <option>Last 7 days</option>
	          <option>Last 30 days</option>
	          <option>Last 90 days</option>
	        </select>
	      </div>
	    </div>
	    <div className="relative h-[280px] w-full">
	      <svg viewBox="0 0 800 280" className="h-full w-full">
	        <g className="grid">
	          <line x1="50" y1="250" x2="750" y2="250" stroke="#e5e7eb" strokeWidth="1" />
	          <line x1="50" y1="200" x2="750" y2="200" stroke="#e5e7eb" strokeWidth="1" />
	          <line x1="50" y1="150" x2="750" y2="150" stroke="#e5e7eb" strokeWidth="1" />
	          <line x1="50" y1="100" x2="750" y2="100" stroke="#e5e7eb" strokeWidth="1" />
	          <line x1="50" y1="50" x2="750" y2="50" stroke="#e5e7eb" strokeWidth="1" />
	          
	          <text x="35" y="250" fontSize="12" textAnchor="end" fill="#6b7280">0</text>
	          <text x="35" y="200" fontSize="12" textAnchor="end" fill="#6b7280">10</text>
	          <text x="35" y="150" fontSize="12" textAnchor="end" fill="#6b7280">20</text>
	          <text x="35" y="100" fontSize="12" textAnchor="end" fill="#6b7280">30</text>
	          <text x="35" y="50" fontSize="12" textAnchor="end" fill="#6b7280">40</text>
	        </g>
	        
	        <polyline 
	          points="50,220 150,190 250,180 350,150 450,120 550,80 650,60 750,40" 
	          fill="none" 
	          stroke="#3b82f6" 
	          strokeWidth="3"
	          strokeLinecap="round"
	          strokeLinejoin="round"
	          className="transition-all duration-700 ease-in-out"
	        />
	        
	        <polyline 
	          points="50,240 150,230 250,210 350,220 450,200 550,210 650,190 750,180" 
	          fill="none" 
	          stroke="#eab308" 
	          strokeWidth="3"
	          strokeLinecap="round"
	          strokeLinejoin="round"
	          className="transition-all duration-700 ease-in-out"
	        />
	        
	        <polyline 
	          points="50,245 150,246 250,242 350,244 450,238 550,240 650,245 750,243" 
	          fill="none" 
	          stroke="#ef4444" 
	          strokeWidth="3"
	          strokeLinecap="round"
	          strokeLinejoin="round"
	          className="transition-all duration-700 ease-in-out"
	        />
	        
	        <g className="x-axis">
	          <text x="50" y="270" fontSize="12" textAnchor="middle" fill="#6b7280">Mon</text>
	          <text x="150" y="270" fontSize="12" textAnchor="middle" fill="#6b7280">Tue</text>
	          <text x="250" y="270" fontSize="12" textAnchor="middle" fill="#6b7280">Wed</text>
	          <text x="350" y="270" fontSize="12" textAnchor="middle" fill="#6b7280">Thu</text>
	          <text x="450" y="270" fontSize="12" textAnchor="middle" fill="#6b7280">Fri</text>
	          <text x="550" y="270" fontSize="12" textAnchor="middle" fill="#6b7280">Sat</text>
	          <text x="650" y="270" fontSize="12" textAnchor="middle" fill="#6b7280">Sun</text>
	          <text x="750" y="270" fontSize="12" textAnchor="middle" fill="#6b7280">Today</text>
	        </g>
	        
	        <g className="data-points">
	          <circle cx="50" cy="220" r="4" fill="#3b82f6" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="150" cy="190" r="4" fill="#3b82f6" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="250" cy="180" r="4" fill="#3b82f6" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="350" cy="150" r="4" fill="#3b82f6" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="450" cy="120" r="4" fill="#3b82f6" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="550" cy="80" r="4" fill="#3b82f6" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="650" cy="60" r="4" fill="#3b82f6" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="750" cy="40" r="4" fill="#3b82f6" className="cursor-pointer hover:r-5 transition-all" />
	          
	          <circle cx="50" cy="240" r="4" fill="#eab308" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="150" cy="230" r="4" fill="#eab308" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="250" cy="210" r="4" fill="#eab308" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="350" cy="220" r="4" fill="#eab308" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="450" cy="200" r="4" fill="#eab308" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="550" cy="210" r="4" fill="#eab308" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="650" cy="190" r="4" fill="#eab308" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="750" cy="180" r="4" fill="#eab308" className="cursor-pointer hover:r-5 transition-all" />
	          
	          <circle cx="50" cy="245" r="4" fill="#ef4444" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="150" cy="246" r="4" fill="#ef4444" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="250" cy="242" r="4" fill="#ef4444" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="350" cy="244" r="4" fill="#ef4444" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="450" cy="238" r="4" fill="#ef4444" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="550" cy="240" r="4" fill="#ef4444" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="650" cy="245" r="4" fill="#ef4444" className="cursor-pointer hover:r-5 transition-all" />
	          <circle cx="750" cy="243" r="4" fill="#ef4444" className="cursor-pointer hover:r-5 transition-all" />
	        </g>
	      </svg>
	    </div>
	  </div>
	
	  <div className="grid grid-cols-4 gap-4">
	    <div className="col-span-1">
	      <div className="rounded-lg bg-white shadow">
	        <div className="border-b border-gray-200 bg-gray-50 p-4">
	          <h3 className="font-semibold text-gray-700">Pending</h3>
	          <div className="mt-1 flex items-center">
	            <span className="rounded-full bg-gray-200 px-2.5 py-0.5 text-xs font-medium text-gray-800">23</span>
	            <div className="ml-auto flex gap-1">
	              <button className="rounded p-1 text-gray-500 transition-colors hover:bg-gray-100 hover:text-gray-700">
	                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
	                  <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"></polygon>
	                </svg>
	              </button>
	              <button className="rounded p-1 text-gray-500 transition-colors hover:bg-gray-100 hover:text-gray-700">
	                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
	                  <circle cx="11" cy="11" r="8"></circle>
	                  <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
	                </svg>
	              </button>
	            </div>
	          </div>
	        </div>
	        <div className="max-h-[600px] overflow-y-auto p-3">
	          <div className="mb-2 cursor-pointer rounded-md border border-gray-200 bg-white p-3 shadow-sm transition-all hover:border-blue-300 hover:shadow">
	            <div className="flex items-start justify-between">
	              <h4 className="font-medium">Annual Report 2023.pdf</h4>
	              <span className="ml-2 rounded-full bg-gray-100 px-2 py-1 text-xs font-medium text-gray-600">PDF</span>
	            </div>
	            <p className="mt-1 text-sm text-gray-500">Uploaded 2 hours ago</p>
	            <div className="mt-2 flex items-center text-xs text-gray-500">
	              <span className="flex items-center">
	                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-1">
	                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
	                  <polyline points="14 2 14 8 20 8"></polyline>
	                </svg>
	                42 pages
	              </span>
	              <span className="ml-3 flex items-center">
	                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-1">
	                  <polyline points="6 9 12 15 18 9"></polyline>
	                </svg>
	                2.3 MB
	              </span>
	            </div>
	          </div>
	          
	          <div className="mb-2 cursor-pointer rounded-md border border-gray-200 bg-white p-3 shadow-sm transition-all hover:border-blue-300 hover:shadow">
	            <div className="flex items-start justify-between">
	              <h4 className="font-medium">Product Specs v2.docx</h4>
	              <span className="ml-2 rounded-full bg-blue-100 px-2 py-1 text-xs font-medium text-blue-600">DOCX</span>
	            </div>
	            <p className="mt-1 text-sm text-gray-500">Uploaded 3 hours ago</p>
	            <div className="mt-2 flex items-center text-xs text-gray-500">
	              <span className="flex items-center">
	                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-1">
	                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
	                  <polyline points="14 2 14 8 20 8"></polyline>
	                </svg>
	                18 pages
	              </span>
	              <span className="ml-3 flex items-center">
	                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-1">
	                  <polyline points="6 9 12 15 18 9"></polyline>
	                </svg>
	                780 KB
	              </span>
	            </div>
	          </div>
	          
	          <div className="mb-2 cursor-pointer rounded-md border border-gray-200 bg-white p-3 shadow-sm transition-all hover:border-blue-300 hover:shadow">
	            <div className="flex items-start justify-between">
	              <h4 className="font-medium">User Research Results.pptx</h4>
	              <span className="ml-2 rounded-full bg-red-100 px-2 py-1 text-xs font-medium text-red-600">PPTX</span>
	            </div>
	            <p className="mt-1 text-sm text-gray-500">Uploaded 5 hours ago</p>
	            <div className="mt-2 flex items-center text-xs text-gray-500">
	              <span className="flex items-center">
	                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-1">
	                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
	                  <polyline points="14 2 14 8 20 8"></polyline>
	                </svg>
	                24 slides
	              </span>
	              <span className="ml-3 flex items-center">
	                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-1">
	                  <polyline points="6 9 12 15 18 9"></polyline>
	                </svg>
	                4.1 MB
	              </span>
	            </div>
	          </div>
	        </div>
	      </div>
	    </div>
	    
	    <div className="col-span-1">
	      <div className="rounded-lg bg-white shadow">
	        <div className="border-b border-gray-200 bg-gray-50 p-4">
	          <h3 className="font-semibold text-gray-700">Processing</h3>
	          <div className="mt-1 flex items-center">
	            <span className="rounded-full bg-blue-200 px-2.5 py-0.5 text-xs font-medium text-blue-800">8</span>
	            <div className="ml-auto flex gap-1">
	              <button className="rounded p-1 text-gray-500 transition-colors hover:bg-gray-100 hover:text-gray-700">
	                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
	                  <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"></polygon>
	                </svg>
	              </button>
	              <button className="rounded p-1 text-gray-500 transition-colors hover:bg-gray-100 hover:text-gray-700">
	                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
	                  <circle cx="11" cy="11" r="8"></circle>
	                  <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
	                </svg>
	              </button>
	            </div>
	          </div>
	        </div>
	        <div className="max-h-[600px] overflow-y-auto p-3">
	          <div className="mb-2 cursor-pointer rounded-md border border-gray-200 bg-white p-3 shadow-sm transition-all hover:border-blue-300 hover:shadow">
	            <div className="flex items-start justify-between">
	              <h4 className="font-medium">Customer Feedback Q1.xlsx</h4>
	              <span className="ml-2 rounded-full bg-green-100 px-2 py-1 text-xs font-medium text-green-600">XLSX</span>
	            </div>
	            <p className="mt-1 text-sm text-gray-500">Processing (45%)</p>
	            <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-gray-200">
	              <div className="h-full w-[45%] rounded-full bg-blue-500"></div>
	            </div>
	            <div className="mt-2 flex items-center text-xs text-gray-500">
	              <span className="flex items-center">
	                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-1">
	                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
	                  <polyline points="14 2 14 8 20 8"></polyline>
	                </svg>
	                5 sheets
	              </span>
	              <span className="ml-3 flex items-center">
	                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-1">
	                  <polyline points="6 9 12 15 18 9"></polyline>
	                </svg>
	                1.8 MB
	              </span>
	            </div>
	          </div>
	          
	          <div className="mb-2 cursor-pointer rounded-md border border-gray-200 bg-white p-3 shadow-sm transition-all hover:border-blue-300 hover:shadow">
	            <div className="flex items-start justify-between">
	              <h4 className="font-medium">Technical Documentation.pdf</h4>
	              <span className="ml-2 rounded-full bg-gray-100 px-2 py-1 text-xs font-medium text-gray-600">PDF</span>
	            </div>
	            <p className="mt-1 text-sm text-gray-500">Processing (72%)</p>
	            <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-gray-200">
	              <div className="h-full w-[72%] rounded-full bg-blue-500"></div>
	            </div>
	            <div className="mt-2 flex items-center text-xs text-gray-500">
	              <span className="flex items-center">
	                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-1">
	                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
	                  <polyline points="14 2 14 8 20 8"></polyline>
	                </svg>
	                87 pages
	              </span>
	              <span className="ml-3 flex items-center">
	                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-1">
	                  <polyline points="6 9 12 15 18 9"></polyline>
	                </svg>
	                8.3 MB
	              </span>
	            </div>
	          </div>
	        </div>
	      </div>
	    </div>
	    
	    <div className="col-span-1">
	      <div className="rounded-lg bg-white shadow">
	        <div className="border-b border-gray-200 bg-gray-50 p-4">
	          <h3 className="font-semibold text-gray-700">Completed</h3>
	          <div className="mt-1 flex items-center">
	            <span className="rounded-full bg-green-200 px-2.5 py-0.5 text-xs font-medium text-green-800">90</span>
	            <div className="ml-auto flex gap-1">
	              <button className="rounded p-1 text-gray-500 transition-colors hover:bg-gray-100 hover:text-gray-700">
	                </button></div></div></div></div></div></div></div> 
        </div>
  )
}

